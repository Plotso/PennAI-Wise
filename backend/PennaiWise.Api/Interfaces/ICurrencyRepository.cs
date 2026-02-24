using PennaiWise.Api.DTOs;

namespace PennaiWise.Api.Interfaces;

public interface ICurrencyRepository
{
    Task<List<CurrencyDto>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string code, CancellationToken ct = default);
    Task<string?> GetSymbolAsync(string code, CancellationToken ct = default);
}
