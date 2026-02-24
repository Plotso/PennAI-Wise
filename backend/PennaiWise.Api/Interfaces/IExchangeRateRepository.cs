using PennaiWise.Api.DTOs;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Interfaces;

public interface IExchangeRateRepository
{
    /// <summary>Returns all exchange rates defined by the given user.</summary>
    Task<List<ExchangeRateDto>> GetUserRatesAsync(int userId, CancellationToken ct = default);

    /// <summary>Returns a tracked entity for update/delete. Null if not found or not owned by user.</summary>
    Task<ExchangeRate?> GetByIdAsync(int id, int userId, CancellationToken ct = default);

    Task AddAsync(ExchangeRate rate, CancellationToken ct = default);
    void Remove(ExchangeRate rate);

    /// <summary>
    /// Finds the most recent rate where <c>EffectiveDate &lt;= asOfDate</c>
    /// for the specified user and currency pair.
    /// </summary>
    Task<ExchangeRate?> GetRateAsync(
        int userId, string fromCode, string toCode, DateTime asOfDate,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether a rate already exists for the exact (user, from, to, effectiveDate) combination.
    /// </summary>
    Task<bool> ExistsDuplicateAsync(
        int userId, string fromCode, string toCode, DateTime effectiveDate,
        int? excludeId = null, CancellationToken ct = default);
}
