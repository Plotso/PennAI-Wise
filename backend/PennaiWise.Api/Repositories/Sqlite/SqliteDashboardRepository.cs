using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteDashboardRepository(AppDbContext context) : IDashboardRepository
{
    public async Task<DashboardDto> GetDashboardAsync(int userId, int month, int year, CancellationToken ct = default)
    {
        var spendingByCategory = await context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date.Month == month && e.Date.Year == year)
            .GroupBy(e => new { e.CategoryId, e.Category.Name, e.Category.Color })
            .Select(g => new CategorySpendingDto(
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.Color,
                g.Sum(e => e.Amount),
                g.Count()))
            .ToListAsync(ct);

        var totalSpent = spendingByCategory.Sum(c => c.TotalAmount);

        return new DashboardDto(month, year, totalSpent, spendingByCategory);
    }
}
