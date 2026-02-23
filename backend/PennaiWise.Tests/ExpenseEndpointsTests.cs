using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PennaiWise.Api.DTOs;
using PennaiWise.Tests.Helpers;

namespace PennaiWise.Tests;

/// <summary>
/// Integration tests for the /api/expenses endpoints.
/// A single <see cref="TestWebAppFactory"/> is shared by all tests in the
/// fixture; state-sensitive tests register isolated users so they never
/// pollute each other's expense list.
/// </summary>
[TestFixture]
public class ExpenseEndpointsTests
{
    private TestWebAppFactory _factory    = null!;
    private HttpClient        _authClient = null!;
    private int               _categoryIdA;
    private int               _categoryIdB;

    // ── Setup / Teardown ─────────────────────────────────────────────────────

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory    = new TestWebAppFactory();
        _authClient = await _factory.CreateAuthenticatedClientAsync();

        // Fetch the seeded global categories so every test has a valid id.
        var categories = await _authClient
            .GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        categories.Should().NotBeNullOrEmpty("seeded categories must exist");
        _categoryIdA = categories![0].Id;
        _categoryIdB = categories.Count > 1 ? categories[1].Id : categories[0].Id;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _authClient.Dispose();
        _factory.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Registers a brand-new user and returns an authenticated client.</summary>
    private async Task<HttpClient> RegisterAndLoginAsync(string email, string password = "TestPass99!")
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterDto(email, password));
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static CreateExpenseDto ValidDto(int categoryId, decimal amount = 12.50m,
        string description = "Test expense", DateTime? date = null)
        => new(amount, description, date ?? DateTime.UtcNow.Date.AddDays(-1), categoryId);

    // ── GET /api/expenses ────────────────────────────────────────────────────

    [Test]
    public async Task GetExpenses_WhenAuthenticated_ReturnsOk()
    {
        var response = await _authClient.GetAsync("/api/expenses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResult<ExpenseDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Test]
    public async Task GetExpenses_WhenNotAuthenticated_Returns401()
    {
        using var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/expenses");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/expenses ───────────────────────────────────────────────────

    [Test]
    public async Task CreateExpense_WithValidData_Returns201()
    {
        var dto = ValidDto(_categoryIdA, 25.00m, "Lunch");

        var response = await _authClient.PostAsJsonAsync("/api/expenses", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ExpenseDto>();
        body.Should().NotBeNull();
        body!.Amount.Should().Be(25.00m);
        body.Description.Should().Be("Lunch");
        body.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CreateExpense_WithNegativeAmount_ReturnsBadRequest()
    {
        var dto = ValidDto(_categoryIdA, -5.00m);

        var response = await _authClient.PostAsJsonAsync("/api/expenses", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("amount");
    }

    // ── PUT /api/expenses/{id} ───────────────────────────────────────────────

    [Test]
    public async Task UpdateExpense_OwnedByUser_ReturnsOk()
    {
        // Create
        var create = await _authClient.PostAsJsonAsync(
            "/api/expenses", ValidDto(_categoryIdA, 10.00m, "Before update"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ExpenseDto>();

        // Update
        var updateDto = new UpdateExpenseDto(
            99.99m, "After update", DateTime.UtcNow.Date.AddDays(-2), _categoryIdA);
        var update = await _authClient.PutAsJsonAsync(
            $"/api/expenses/{created!.Id}", updateDto);

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<ExpenseDto>();
        updated!.Amount.Should().Be(99.99m);
        updated.Description.Should().Be("After update");
    }

    [Test]
    public async Task UpdateExpense_OwnedByOtherUser_Returns404()
    {
        // Register a second user and create an expense as them.
        using var otherClient = await RegisterAndLoginAsync(
            $"other_{Guid.NewGuid():N}@test.com");

        var create = await otherClient.PostAsJsonAsync(
            "/api/expenses", ValidDto(_categoryIdA, 7.00m, "Other user's expense"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ExpenseDto>();

        // Try to update it as the main (seeded) test user → 404.
        var updateDto = new UpdateExpenseDto(
            1.00m, "Hijacked", DateTime.UtcNow.Date.AddDays(-1), _categoryIdA);
        var response = await _authClient.PutAsJsonAsync(
            $"/api/expenses/{created!.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/expenses/{id} ────────────────────────────────────────────

    [Test]
    public async Task DeleteExpense_OwnedByUser_Returns204()
    {
        // Create an expense specifically to delete.
        var create = await _authClient.PostAsJsonAsync(
            "/api/expenses", ValidDto(_categoryIdA, 5.00m, "To be deleted"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ExpenseDto>();

        var delete = await _authClient.DeleteAsync($"/api/expenses/{created!.Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Filtering ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetExpenses_FilterByDateRange_ReturnsFiltered()
    {
        // Use an isolated user so only our two expenses exist.
        using var client = await RegisterAndLoginAsync(
            $"datefilter_{Guid.NewGuid():N}@test.com");

        // Ensure the user can see categories.
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var catId = categories![0].Id;

        var jan = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var mar = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(10m, "January expense", jan,  catId));
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(20m, "March expense",   mar,  catId));

        // ── Filter to include only March ─────────────────────────────────────
        var startMar = Uri.EscapeDataString("2024-03-01T00:00:00Z");
        var endMar   = Uri.EscapeDataString("2024-03-31T23:59:59Z");
        var result = await client.GetFromJsonAsync<PaginatedResult<ExpenseDto>>(
            $"/api/expenses?startDate={startMar}&endDate={endMar}");

        result!.TotalCount.Should().Be(1);
        result.Items[0].Description.Should().Be("March expense");

        // ── Filter to include only January ───────────────────────────────────
        var startJan = Uri.EscapeDataString("2024-01-01T00:00:00Z");
        var endJan   = Uri.EscapeDataString("2024-01-31T23:59:59Z");
        var resultJan = await client.GetFromJsonAsync<PaginatedResult<ExpenseDto>>(
            $"/api/expenses?startDate={startJan}&endDate={endJan}");

        resultJan!.TotalCount.Should().Be(1);
        resultJan.Items[0].Description.Should().Be("January expense");
    }

    [Test]
    public async Task GetExpenses_FilterByCategory_ReturnsFiltered()
    {
        // Use an isolated user.
        using var client = await RegisterAndLoginAsync(
            $"catfilter_{Guid.NewGuid():N}@test.com");

        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        categories.Should().HaveCountGreaterThan(1, "at least two seeded categories are needed");

        var catA = categories![0].Id;
        var catB = categories[1].Id;

        var past = DateTime.UtcNow.Date.AddDays(-1);
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(10m, "In category A", past, catA));
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(20m, "In category B", past, catB));

        var result = await client.GetFromJsonAsync<PaginatedResult<ExpenseDto>>(
            $"/api/expenses?categoryId={catA}");

        result!.TotalCount.Should().Be(1);
        result.Items[0].CategoryId.Should().Be(catA);
        result.Items[0].Description.Should().Be("In category A");
    }
}
