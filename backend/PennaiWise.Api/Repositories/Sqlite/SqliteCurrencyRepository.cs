using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteCurrencyRepository(AppDbContext context) : ICurrencyRepository
{
    public async Task<List<CurrencyDto>> GetAllAsync(CancellationToken ct = default) =>
        await context.Currencies
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Code, c.Name, c.Symbol))
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(string code, CancellationToken ct = default) =>
        context.Currencies.AsNoTracking().AnyAsync(c => c.Code == code, ct);

    public Task<string?> GetSymbolAsync(string code, CancellationToken ct = default) =>
        context.Currencies.AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => (string?)c.Symbol)
            .FirstOrDefaultAsync(ct);
}
