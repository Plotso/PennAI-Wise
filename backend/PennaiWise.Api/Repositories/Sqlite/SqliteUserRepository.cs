using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteUserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public Task<string?> GetDefaultCurrencyCodeAsync(int userId, CancellationToken ct = default) =>
        context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.DefaultCurrencyCode)
            .FirstOrDefaultAsync(ct);

    public async Task UpdateDefaultCurrencyAsync(int userId, string? currencyCode, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is not null)
        {
            user.DefaultCurrencyCode = currencyCode;
        }
    }
}
