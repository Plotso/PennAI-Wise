using PennaiWise.Api.Models;

namespace PennaiWise.Api.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<string?> GetDefaultCurrencyCodeAsync(int userId, CancellationToken ct = default);
    Task UpdateDefaultCurrencyAsync(int userId, string? currencyCode, CancellationToken ct = default);
}
