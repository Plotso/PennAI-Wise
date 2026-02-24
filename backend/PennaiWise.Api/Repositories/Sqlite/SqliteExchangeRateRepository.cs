using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteExchangeRateRepository(AppDbContext context) : IExchangeRateRepository
{
    public async Task<List<ExchangeRateDto>> GetUserRatesAsync(int userId, CancellationToken ct = default) =>
        await context.ExchangeRates
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.EffectiveDate)
            .ThenBy(r => r.FromCurrencyCode)
            .ThenBy(r => r.ToCurrencyCode)
            .Select(r => new ExchangeRateDto(
                r.Id,
                r.FromCurrencyCode,
                r.ToCurrencyCode,
                r.Rate,
                r.EffectiveDate))
            .ToListAsync(ct);

    public Task<ExchangeRate?> GetByIdAsync(int id, int userId, CancellationToken ct = default) =>
        context.ExchangeRates
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

    public async Task AddAsync(ExchangeRate rate, CancellationToken ct = default) =>
        await context.ExchangeRates.AddAsync(rate, ct);

    public void Remove(ExchangeRate rate) =>
        context.ExchangeRates.Remove(rate);

    public Task<ExchangeRate?> GetRateAsync(
        int userId, string fromCode, string toCode, DateTime asOfDate,
        CancellationToken ct = default) =>
        context.ExchangeRates
            .AsNoTracking()
            .Where(r => r.UserId == userId
                     && r.FromCurrencyCode == fromCode
                     && r.ToCurrencyCode == toCode
                     && r.EffectiveDate <= asOfDate)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsDuplicateAsync(
        int userId, string fromCode, string toCode, DateTime effectiveDate,
        int? excludeId = null, CancellationToken ct = default)
    {
        var query = context.ExchangeRates
            .AsNoTracking()
            .Where(r => r.UserId == userId
                     && r.FromCurrencyCode == fromCode
                     && r.ToCurrencyCode == toCode
                     && r.EffectiveDate == effectiveDate);

        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        return query.AnyAsync(ct);
    }
}
