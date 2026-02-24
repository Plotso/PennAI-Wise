using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PennaiWise.Api.DTOs;
using PennaiWise.Tests.Helpers;

namespace PennaiWise.Tests;

/// <summary>
/// Integration tests for the /api/exchange-rates and /api/settings endpoints,
/// as well as currency-related dashboard behaviour.
/// </summary>
[TestFixture]
public class CurrencyFeatureTests
{
    private TestWebAppFactory _factory = null!;
    private HttpClient _client         = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestWebAppFactory();
        _client  = await RegisterAndLoginAsync($"currency_{Guid.NewGuid():N}@test.com");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<HttpClient> RegisterAndLoginAsync(string email, string password = "TestPass99!")
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto(email, password));
        resp.EnsureSuccessStatusCode();
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    // ── GET /api/currencies ──────────────────────────────────────────────────

    [Test]
    public async Task GetCurrencies_ReturnsSupportedCurrencies()
    {
        var resp = await _client.GetAsync("/api/currencies");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await resp.Content.ReadFromJsonAsync<List<CurrencyDto>>();
        list.Should().NotBeNull();
        list!.Count.Should().BeGreaterThanOrEqualTo(3);
        list.Should().Contain(c => c.Code == "EUR");
        list.Should().Contain(c => c.Code == "USD");
        list.Should().Contain(c => c.Code == "GBP");
    }

    // ── GET/PUT /api/settings ────────────────────────────────────────────────

    [Test]
    public async Task Settings_DefaultCurrencyIsNull_ThenUpdatable()
    {
        // Default should be null
        var getResp = await _client.GetAsync("/api/settings");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await getResp.Content.ReadFromJsonAsync<UserSettingsDto>();
        settings!.DefaultCurrencyCode.Should().BeNull();

        // Set to USD
        var putResp = await _client.PutAsJsonAsync("/api/settings", new UserSettingsDto("USD"));
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify
        var verifyResp = await _client.GetAsync("/api/settings");
        var updated = await verifyResp.Content.ReadFromJsonAsync<UserSettingsDto>();
        updated!.DefaultCurrencyCode.Should().Be("USD");

        // Reset to null
        await _client.PutAsJsonAsync("/api/settings", new UserSettingsDto(null));
    }

    [Test]
    public async Task Settings_InvalidCurrency_ReturnsBadRequest()
    {
        var resp = await _client.PutAsJsonAsync("/api/settings", new UserSettingsDto("XYZ"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── CRUD /api/exchange-rates ─────────────────────────────────────────────

    [Test]
    public async Task ExchangeRates_CrudCycle()
    {
        // Create
        var createDto = new CreateExchangeRateDto("EUR", "USD", 1.10m, new DateTime(2025, 1, 1));
        var createResp = await _client.PostAsJsonAsync("/api/exchange-rates", createDto);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<ExchangeRateDto>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.FromCurrencyCode.Should().Be("EUR");
        created.ToCurrencyCode.Should().Be("USD");
        created.Rate.Should().Be(1.10m);

        // List
        var listResp = await _client.GetAsync("/api/exchange-rates");
        var rates = await listResp.Content.ReadFromJsonAsync<List<ExchangeRateDto>>();
        rates.Should().Contain(r => r.Id == created.Id);

        // Update
        var updateDto = new UpdateExchangeRateDto(1.15m, new DateTime(2025, 1, 1));
        var updateResp = await _client.PutAsJsonAsync($"/api/exchange-rates/{created.Id}", updateDto);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<ExchangeRateDto>();
        updated!.Rate.Should().Be(1.15m);

        // Delete
        var deleteResp = await _client.DeleteAsync($"/api/exchange-rates/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task ExchangeRates_SameCurrencyPair_ReturnsBadRequest()
    {
        var dto = new CreateExchangeRateDto("EUR", "EUR", 1.0m, DateTime.UtcNow.Date);
        var resp = await _client.PostAsJsonAsync("/api/exchange-rates", dto);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ExchangeRates_InvalidCurrency_ReturnsBadRequest()
    {
        var dto = new CreateExchangeRateDto("EUR", "XYZ", 1.0m, DateTime.UtcNow.Date);
        var resp = await _client.PostAsJsonAsync("/api/exchange-rates", dto);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ExchangeRates_NegativeRate_ReturnsBadRequest()
    {
        var dto = new CreateExchangeRateDto("EUR", "USD", -1.0m, DateTime.UtcNow.Date);
        var resp = await _client.PostAsJsonAsync("/api/exchange-rates", dto);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Expense with currency ────────────────────────────────────────────────

    [Test]
    public async Task CreateExpense_WithCurrency_ReturnsCurrencyInfo()
    {
        var cats = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var catId = cats![0].Id;

        var dto = new CreateExpenseDto(50.00m, "USD expense", DateTime.UtcNow.Date.AddDays(-1), catId, "USD");
        var resp = await _client.PostAsJsonAsync("/api/expenses", dto);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var expense = await resp.Content.ReadFromJsonAsync<ExpenseDto>();
        expense!.CurrencyCode.Should().Be("USD");
        expense.CurrencySymbol.Should().Be("$");
    }

    [Test]
    public async Task CreateExpense_WithInvalidCurrency_ReturnsBadRequest()
    {
        var cats = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var catId = cats![0].Id;

        var dto = new CreateExpenseDto(50.00m, "Bad currency", DateTime.UtcNow.Date.AddDays(-1), catId, "XYZ");
        var resp = await _client.PostAsJsonAsync("/api/expenses", dto);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Dashboard currency conversion ────────────────────────────────────────

    [Test]
    public async Task Dashboard_ConvertsCurrencyUsingExchangeRate()
    {
        // Set up an isolated user for this test
        using var client = await RegisterAndLoginAsync($"dashconv_{Guid.NewGuid():N}@test.com");

        var cats = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var catId = cats![0].Id;

        var today = DateTime.UtcNow.Date;

        // Create a EUR expense and a USD expense
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(100.00m, "EUR expense", today.AddDays(-1), catId, "EUR"));
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(50.00m, "USD expense", today.AddDays(-1), catId, "USD"));

        // Add exchange rate: EUR → USD = 1.10, effective before the expense date
        await client.PostAsJsonAsync("/api/exchange-rates",
            new CreateExchangeRateDto("EUR", "USD", 1.10m, today.AddDays(-5)));

        // Get dashboard in USD
        var resp = await client.GetAsync($"/api/dashboard?currency=USD");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dash = await resp.Content.ReadFromJsonAsync<DashboardDto>();
        dash!.DisplayCurrency.Should().Be("USD");
        // EUR 100 → USD 110, plus USD 50 = 160
        dash.TotalSpent.Should().Be(160.00m);
    }

    [Test]
    public async Task Dashboard_UsesDateAwareExchangeRate()
    {
        // Set up an isolated user for this test
        using var client = await RegisterAndLoginAsync($"dashdate_{Guid.NewGuid():N}@test.com");

        var cats = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        var catId = cats![0].Id;

        var today = DateTime.UtcNow.Date;

        // Add two exchange rates at different dates
        // Rate at Jan 1: EUR→USD = 1.05
        await client.PostAsJsonAsync("/api/exchange-rates",
            new CreateExchangeRateDto("EUR", "USD", 1.05m, new DateTime(today.Year, today.Month, 1)));
        // Rate at today-3: EUR→USD = 1.20
        await client.PostAsJsonAsync("/api/exchange-rates",
            new CreateExchangeRateDto("EUR", "USD", 1.20m, today.AddDays(-3)));

        // Create a EUR expense on the 2nd day of the month (should use 1.05 rate from the 1st)
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(100.00m, "Early expense", new DateTime(today.Year, today.Month, 2), catId, "EUR"));

        // Create a EUR expense yesterday (should use 1.20 rate from today-3)
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseDto(100.00m, "Late expense", today.AddDays(-1), catId, "EUR"));

        // Dashboard in USD
        var resp = await client.GetAsync($"/api/dashboard?currency=USD");
        var dash = await resp.Content.ReadFromJsonAsync<DashboardDto>();

        // Early: 100 * 1.05 = 105, Late: 100 * 1.20 = 120, Total = 225
        dash!.TotalSpent.Should().Be(225.00m);
    }

    [Test]
    public async Task Dashboard_DefaultCurrencyFallsBackToEur()
    {
        using var client = await RegisterAndLoginAsync($"dashdef_{Guid.NewGuid():N}@test.com");

        var resp = await client.GetAsync("/api/dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dash = await resp.Content.ReadFromJsonAsync<DashboardDto>();
        dash!.DisplayCurrency.Should().Be("EUR");
    }

    [Test]
    public async Task Dashboard_UsesUserDefaultCurrency()
    {
        using var client = await RegisterAndLoginAsync($"dashuser_{Guid.NewGuid():N}@test.com");

        // Set user default to GBP
        await client.PutAsJsonAsync("/api/settings", new UserSettingsDto("GBP"));

        var resp = await client.GetAsync("/api/dashboard");
        var dash = await resp.Content.ReadFromJsonAsync<DashboardDto>();
        dash!.DisplayCurrency.Should().Be("GBP");
    }
}
