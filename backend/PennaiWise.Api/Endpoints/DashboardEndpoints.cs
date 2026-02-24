using PennaiWise.Api.Extensions;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", GetDashboardAsync)
           .WithName("GetDashboard")
           .WithSummary("Get aggregated spending data for a given month/year, converted to a display currency")
           .RequireAuthorization();
    }

    private static async Task<IResult> GetDashboardAsync(
        HttpContext http,
        IDashboardRepository dashboard,
        IUserRepository users,
        ICurrencyRepository currencies,
        int? month,
        int? year,
        string? currency,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var resolvedMonth = month ?? now.Month;
        var resolvedYear  = year  ?? now.Year;

        if (resolvedMonth is < 1 or > 12)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "month", ["Month must be between 1 and 12."] }
            });

        if (resolvedYear < 2000 || resolvedYear > now.Year + 1)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "year", [$"Year must be between 2000 and {now.Year + 1}."] }
            });

        // Resolve display currency: query param > user default > EUR fallback
        var displayCurrency = currency?.ToUpperInvariant()
                           ?? await users.GetDefaultCurrencyCodeAsync(userId, ct)
                           ?? "EUR";

        // Validate the currency code exists
        if (!await currencies.ExistsAsync(displayCurrency, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "currency", [$"Currency '{displayCurrency}' not found."] }
            });

        var displaySymbol = await currencies.GetSymbolAsync(displayCurrency, ct) ?? displayCurrency;

        var result = await dashboard.GetDashboardAsync(
            userId, resolvedMonth, resolvedYear, displayCurrency, displaySymbol, ct);

        return Results.Ok(result);
    }
}
