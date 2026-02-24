using PennaiWise.Api.DTOs;
using PennaiWise.Api.Extensions;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Endpoints;

public static class ExchangeRateEndpoints
{
    public static void MapExchangeRateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exchange-rates")
                       .RequireAuthorization();

        group.MapGet("/",            GetAllAsync)
             .WithName("GetExchangeRates")
             .WithSummary("Get all exchange rates for the current user");

        group.MapPost("/",           CreateAsync)
             .WithName("CreateExchangeRate")
             .WithSummary("Create a new exchange rate");

        group.MapPut("/{id:int}",    UpdateAsync)
             .WithName("UpdateExchangeRate")
             .WithSummary("Update an existing exchange rate");

        group.MapDelete("/{id:int}", DeleteAsync)
             .WithName("DeleteExchangeRate")
             .WithSummary("Delete an exchange rate");
    }

    // ── GET /api/exchange-rates ──────────────────────────────────────────────

    private static async Task<IResult> GetAllAsync(
        HttpContext http,
        IExchangeRateRepository rates,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var result = await rates.GetUserRatesAsync(userId, ct);
        return Results.Ok(result);
    }

    // ── POST /api/exchange-rates ─────────────────────────────────────────────

    private static async Task<IResult> CreateAsync(
        CreateExchangeRateDto dto,
        HttpContext http,
        IExchangeRateRepository rates,
        ICurrencyRepository currencies,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        if (ValidationError(dto.FromCurrencyCode, dto.ToCurrencyCode, dto.Rate) is { } err)
            return err;

        if (!await currencies.ExistsAsync(dto.FromCurrencyCode, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "fromCurrencyCode", [$"Currency '{dto.FromCurrencyCode}' not found."] }
            });

        if (!await currencies.ExistsAsync(dto.ToCurrencyCode, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "toCurrencyCode", [$"Currency '{dto.ToCurrencyCode}' not found."] }
            });

        if (await rates.ExistsDuplicateAsync(userId, dto.FromCurrencyCode, dto.ToCurrencyCode, dto.EffectiveDate, ct: ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "effectiveDate", ["A rate for this currency pair and date already exists."] }
            });

        var rate = new ExchangeRate
        {
            FromCurrencyCode = dto.FromCurrencyCode.ToUpperInvariant(),
            ToCurrencyCode   = dto.ToCurrencyCode.ToUpperInvariant(),
            Rate             = dto.Rate,
            EffectiveDate    = dto.EffectiveDate,
            UserId           = userId
        };

        await rates.AddAsync(rate, ct);
        await uow.SaveChangesAsync(ct);

        var result = new ExchangeRateDto(rate.Id, rate.FromCurrencyCode, rate.ToCurrencyCode, rate.Rate, rate.EffectiveDate);
        return Results.Created($"/api/exchange-rates/{rate.Id}", result);
    }

    // ── PUT /api/exchange-rates/{id} ─────────────────────────────────────────

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateExchangeRateDto dto,
        HttpContext http,
        IExchangeRateRepository rates,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var rate = await rates.GetByIdAsync(id, userId, ct);
        if (rate is null)
            return Results.NotFound();

        if (dto.Rate <= 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "rate", ["Rate must be greater than zero."] }
            });

        // Check for duplicate with updated effective date (exclude self)
        if (await rates.ExistsDuplicateAsync(
                userId, rate.FromCurrencyCode, rate.ToCurrencyCode, dto.EffectiveDate, excludeId: id, ct: ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "effectiveDate", ["A rate for this currency pair and date already exists."] }
            });

        rate.Rate          = dto.Rate;
        rate.EffectiveDate = dto.EffectiveDate;

        await uow.SaveChangesAsync(ct);

        var result = new ExchangeRateDto(rate.Id, rate.FromCurrencyCode, rate.ToCurrencyCode, rate.Rate, rate.EffectiveDate);
        return Results.Ok(result);
    }

    // ── DELETE /api/exchange-rates/{id} ──────────────────────────────────────

    private static async Task<IResult> DeleteAsync(
        int id,
        HttpContext http,
        IExchangeRateRepository rates,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var rate = await rates.GetByIdAsync(id, userId, ct);
        if (rate is null)
            return Results.NotFound();

        rates.Remove(rate);
        await uow.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ── Shared validation ────────────────────────────────────────────────────

    private static IResult? ValidationError(string from, string to, decimal rate)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(from))
            errors["fromCurrencyCode"] = ["From currency is required."];

        if (string.IsNullOrWhiteSpace(to))
            errors["toCurrencyCode"] = ["To currency is required."];

        if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to)
            && string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            errors["toCurrencyCode"] = ["From and To currencies must be different."];

        if (rate <= 0)
            errors["rate"] = ["Rate must be greater than zero."];

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }
}
