namespace PennaiWise.Api.Interfaces;

/// <summary>
/// Service that resolves exchange rates and converts amounts between currencies.
/// Implementations may read from the local DB (user-defined rates) or fetch from
/// an external API (future extension).
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Returns the exchange rate applicable for converting <paramref name="fromCurrency"/>
    /// to <paramref name="toCurrency"/> as of the given date.
    /// Uses the most recent rate whose effective date is on or before <paramref name="asOfDate"/>.
    /// Falls back to 1.0 if no rate is found.
    /// </summary>
    Task<decimal> GetRateAsync(
        int userId, string fromCurrency, string toCurrency, DateTime asOfDate,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience method: <c>amount * GetRateAsync(...)</c>.
    /// </summary>
    Task<decimal> ConvertAsync(
        int userId, decimal amount, string fromCurrency, string toCurrency, DateTime asOfDate,
        CancellationToken ct = default);
}
