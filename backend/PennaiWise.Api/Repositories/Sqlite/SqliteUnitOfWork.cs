using PennaiWise.Api.Data;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
