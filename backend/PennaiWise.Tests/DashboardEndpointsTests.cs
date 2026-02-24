using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PennaiWise.Api.DTOs;
using PennaiWise.Tests.Helpers;

namespace PennaiWise.Tests;

/// <summary>
/// Integration tests for GET /api/dashboard.
/// Each test class method that requires deterministic totals registers its own
/// isolated user so expense state from other test fixtures never leaks in.
/// </summary>
[TestFixture]
public class DashboardEndpointsTests
{
    private TestWebAppFactory _factory      = null!;
    private HttpClient        _dashClient   = null!;
    private decimal           _expectedTotal;
    private int               _expectedCount;
    private int               _catIdA;
    private int               _catIdB;

    // ── Setup / Teardown ─────────────────────────────────────────────────────

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestWebAppFactory();

        // Register a dedicated dashboard user (not the seeded test user) so
        // this fixture starts with a completely empty expense ledger.
        _dashClient = await RegisterAndLoginAsync(
            $"dashboard_{Guid.NewGuid():N}@test.com");

        var categories = await _dashClient
            .GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        categories.Should().HaveCountGreaterThan(1, "need at least two categories for breakdown tests");
        _catIdA = categories![0].Id;
        _catIdB = categories[1].Id;

        // Seed exactly two current-month expenses for assertions.
        var today = DateTime.UtcNow.Date;

        await _dashClient.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(10.00m, "Dashboard expense A", today.AddDays(-1), _catIdA));

        await _dashClient.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(35.00m, "Dashboard expense B", today.AddDays(-2), _catIdB));

        _expectedTotal = 45.00m;
        _expectedCount = 2;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dashClient.Dispose();
        _factory.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    // ── Tests ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetDashboard_ReturnsCorrectTotals()
    {
        var response = await _dashClient.GetAsync("/api/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DashboardDto>();
        body.Should().NotBeNull();
        body!.TotalSpent.Should().Be(_expectedTotal);
        body.TransactionCount.Should().Be(_expectedCount);
        body.DisplayCurrency.Should().Be("EUR");
        body.DisplayCurrencySymbol.Should().Be("€");
    }

    [Test]
    public async Task GetDashboard_ReturnsCorrectCategoryBreakdown()
    {
        var response = await _dashClient.GetAsync("/api/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DashboardDto>();
        body.Should().NotBeNull();

        // Both categories must appear in the breakdown.
        body!.CategoryBreakdown.Should().HaveCount(2);

        // Percentages must sum to 100 (allow floating-point rounding).
        var totalPct = body.CategoryBreakdown.Sum(c => c.Percentage);
        totalPct.Should().BeApproximately(100.0, precision: 0.01);

        // Category A: 10 / 45 ≈ 22.22 %
        var breakdownA = body.CategoryBreakdown
            .FirstOrDefault(c => c.CategoryName == _catIdA.ToString()
                || body.CategoryBreakdown
                       .OrderBy(x => x.Total)
                       .First()
                       .CategoryName == c.CategoryName);

        body.CategoryBreakdown
            .Should().Contain(c => c.Total == 10.00m,
                because: "the 10.00 expense should appear in breakdown");

        body.CategoryBreakdown
            .Should().Contain(c => c.Total == 35.00m,
                because: "the 35.00 expense should appear in breakdown");
    }

    [Test]
    public async Task GetDashboard_WithNoExpenses_ReturnsZeros()
    {
        // Register a fresh user who has never created an expense.
        using var freshClient = await RegisterAndLoginAsync(
            $"nodash_{Guid.NewGuid():N}@test.com");

        var response = await freshClient.GetAsync("/api/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DashboardDto>();
        body.Should().NotBeNull();
        body!.TotalSpent.Should().Be(0m);
        body.TransactionCount.Should().Be(0);
        body.CategoryBreakdown.Should().BeEmpty();
        body.HighestExpense.Should().BeNull();
    }
}
