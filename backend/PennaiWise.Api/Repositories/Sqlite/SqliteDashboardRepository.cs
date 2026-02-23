using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteDashboardRepository(AppDbContext context) : IDashboardRepository
{
    public async Task<DashboardDto> GetDashboardAsync(int userId, int month, int year, CancellationToken ct = default)
    {
        var baseQuery = context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date.Month == month && e.Date.Year == year);

        var totalSpent       = await baseQuery.SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;
        var transactionCount = await baseQuery.CountAsync(ct);

        // Highest single expense as a DTO — sort after ToList to avoid the
        // EF_DECIMAL collation bug when ordering by a decimal column in SQLite.
        var highestExpense = (await baseQuery
            .Select(e => new ExpenseDto(
                e.Id, e.Amount, e.Description, e.Date,
                e.CategoryId, e.Category.Name, e.Category.Color, e.CreatedAt))
            .ToListAsync(ct))
            .OrderByDescending(e => e.Amount)
            .FirstOrDefault();

        // Category breakdown — fetch unsorted and sort client-side to avoid the
        // EF_DECIMAL collation crash that SQLite triggers when ORDER BY is applied
        // directly to a decimal aggregate column (EF Core 10 + Microsoft.Data.Sqlite).
        var categoryGroups = (await baseQuery
            .GroupBy(e => new { e.Category.Name, e.Category.Color })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Color,
                Total = g.Sum(e => e.Amount)
            })
            .ToListAsync(ct))
            .OrderByDescending(g => g.Total)
            .ToList();

        var topCategory = categoryGroups.Count > 0 ? categoryGroups[0].Name : null;

        var categoryBreakdown = categoryGroups
            .Select(g => new CategorySpendingDto(
                g.Name,
                g.Color,
                g.Total,
                totalSpent == 0 ? 0d : Math.Round((double)(g.Total / totalSpent) * 100, 2)))
            .ToList();

        // Daily spending totals — group on the client side so that Date.Date
        // projection works with every EF Core provider (InMemory, SQLite, etc.).
        var rawForDaily = await baseQuery
            .Select(e => new { e.Date, e.Amount })
            .ToListAsync(ct);

        var dailySpending = rawForDaily
            .GroupBy(e => e.Date.Date)
            .Select(g => new DailySpendingDto(g.Key, g.Sum(e => e.Amount)))
            .OrderBy(d => d.Date)
            .ToList();

        return new DashboardDto(
            totalSpent,
            transactionCount,
            highestExpense,
            topCategory,
            categoryBreakdown,
            dailySpending);
    }
}
