using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Endpoints;

public static class CurrencyEndpoints
{
    public static void MapCurrencyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/currencies", GetAllAsync)
           .WithName("GetCurrencies")
           .WithSummary("Get all supported currencies")
           .RequireAuthorization();
    }

    private static async Task<IResult> GetAllAsync(
        ICurrencyRepository currencies,
        CancellationToken ct)
    {
        var result = await currencies.GetAllAsync(ct);
        return Results.Ok(result);
    }
}
