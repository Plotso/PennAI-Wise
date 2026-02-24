using PennaiWise.Api.DTOs;
using PennaiWise.Api.Extensions;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings")
                       .RequireAuthorization();

        group.MapGet("/",  GetSettingsAsync)
             .WithName("GetSettings")
             .WithSummary("Get user settings (default currency, etc.)");

        group.MapPut("/",  UpdateSettingsAsync)
             .WithName("UpdateSettings")
             .WithSummary("Update user settings");
    }

    // ── GET /api/settings ────────────────────────────────────────────────────

    private static async Task<IResult> GetSettingsAsync(
        HttpContext http,
        IUserRepository users,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var currencyCode = await users.GetDefaultCurrencyCodeAsync(userId, ct);
        return Results.Ok(new UserSettingsDto(currencyCode));
    }

    // ── PUT /api/settings ────────────────────────────────────────────────────

    private static async Task<IResult> UpdateSettingsAsync(
        UserSettingsDto dto,
        HttpContext http,
        IUserRepository users,
        ICurrencyRepository currencies,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        // Validate currency code if provided
        if (dto.DefaultCurrencyCode is not null
            && !await currencies.ExistsAsync(dto.DefaultCurrencyCode, ct))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "defaultCurrencyCode", [$"Currency '{dto.DefaultCurrencyCode}' not found."] }
            });
        }

        await users.UpdateDefaultCurrencyAsync(userId, dto.DefaultCurrencyCode, ct);
        await uow.SaveChangesAsync(ct);

        return Results.Ok(dto);
    }
}
