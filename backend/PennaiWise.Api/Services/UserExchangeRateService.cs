using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Services;

/// <summary>
/// Resolves exchange rates from user-defined entries in the database.
/// <para>
/// Lookup strategy for a given (from, to, asOfDate):
/// 1. Find a direct rate where <c>EffectiveDate &lt;= asOfDate</c>, most recent first.
/// 2. If not found, try the reverse pair and invert (1 / rate).
/// 3. If neither exists, fall back to 1.0 (identity — no conversion).
/// </para>
/// <para>
/// To switch to an external rate provider in the future, implement
/// <see cref="IExchangeRateService"/> in a new class and swap the DI registration.
/// </para>
/// </summary>
public class UserExchangeRateService(IExchangeRateRepository exchangeRateRepo) : IExchangeRateService
{
    public async Task<decimal> GetRateAsync(
        int userId, string fromCurrency, string toCurrency, DateTime asOfDate,
        CancellationToken ct = default)
    {
        // Same currency → no conversion needed.
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        // 1. Direct lookup: from → to
        var direct = await exchangeRateRepo.GetRateAsync(userId, fromCurrency, toCurrency, asOfDate, ct);
        if (direct is not null)
            return direct.Rate;

        // 2. Reverse lookup: to → from, invert
        var reverse = await exchangeRateRepo.GetRateAsync(userId, toCurrency, fromCurrency, asOfDate, ct);
        if (reverse is not null && reverse.Rate != 0)
            return Math.Round(1.0m / reverse.Rate, 6);

        // 3. Fallback: 1:1
        return 1.0m;
    }

    public async Task<decimal> ConvertAsync(
        int userId, decimal amount, string fromCurrency, string toCurrency, DateTime asOfDate,
        CancellationToken ct = default)
    {
        var rate = await GetRateAsync(userId, fromCurrency, toCurrency, asOfDate, ct);
        return Math.Round(amount * rate, 2);
    }
}
