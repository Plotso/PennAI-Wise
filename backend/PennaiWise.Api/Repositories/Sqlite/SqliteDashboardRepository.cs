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

        // Highest single expense as a DTO
        var highestExpense = await baseQuery
            .OrderByDescending(e => e.Amount)
            .Select(e => new ExpenseDto(
                e.Id, e.Amount, e.Description, e.Date,
                e.CategoryId, e.Category.Name, e.Category.Color, e.CreatedAt))
            .FirstOrDefaultAsync(ct);

        // Category breakdown — computed at DB level, sorted descending by spend
        var categoryGroups = await baseQuery
            .GroupBy(e => new { e.Category.Name, e.Category.Color })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Color,
                Total = g.Sum(e => e.Amount)
            })
            .OrderByDescending(g => g.Total)
            .ToListAsync(ct);

        var topCategory = categoryGroups.Count > 0 ? categoryGroups[0].Name : null;

        var categoryBreakdown = categoryGroups
            .Select(g => new CategorySpendingDto(
                g.Name,
                g.Color,
                g.Total,
                totalSpent == 0 ? 0d : Math.Round((double)(g.Total / totalSpent) * 100, 2)))
            .ToList();

        // Daily spending totals
        var dailySpending = await baseQuery
            .GroupBy(e => e.Date.Date)
            .Select(g => new DailySpendingDto(g.Key, g.Sum(e => e.Amount)))
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        return new DashboardDto(
            totalSpent,
            transactionCount,
            highestExpense,
            topCategory,
            categoryBreakdown,
            dailySpending);
    }
}
