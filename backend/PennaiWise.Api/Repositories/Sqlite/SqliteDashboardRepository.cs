using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteDashboardRepository(AppDbContext context, IExchangeRateService exchangeRateService) : IDashboardRepository
{
    public async Task<DashboardDto> GetDashboardAsync(
        int userId, int month, int year, string displayCurrency, string displayCurrencySymbol,
        CancellationToken ct = default)
    {
        // Fetch all expenses for the month including currency info.
        var expenses = await context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date.Month == month && e.Date.Year == year)
            .Select(e => new
            {
                e.Id,
                e.Amount,
                e.Description,
                e.Date,
                e.CategoryId,
                CategoryName = e.Category.Name,
                CategoryColor = e.Category.Color,
                e.CurrencyCode,
                CurrencySymbol = e.Currency.Symbol,
                e.CreatedAt
            })
            .ToListAsync(ct);

        if (expenses.Count == 0)
        {
            return new DashboardDto(0m, 0, null, null, displayCurrency, displayCurrencySymbol, [], []);
        }

        // Convert each expense amount to the display currency.
        var converted = new List<(
            int Id, decimal OriginalAmount, decimal ConvertedAmount,
            string Description, DateTime Date, int CategoryId,
            string CategoryName, string? CategoryColor,
            string CurrencyCode, string CurrencySymbol, DateTime CreatedAt)>();

        foreach (var e in expenses)
        {
            var convertedAmount = await exchangeRateService.ConvertAsync(
                userId, e.Amount, e.CurrencyCode, displayCurrency, e.Date, ct);

            converted.Add((e.Id, e.Amount, convertedAmount, e.Description, e.Date,
                e.CategoryId, e.CategoryName, e.CategoryColor,
                e.CurrencyCode, e.CurrencySymbol, e.CreatedAt));
        }

        var totalSpent       = converted.Sum(e => e.ConvertedAmount);
        var transactionCount = converted.Count;

        // Highest expense (by converted amount)
        var highest = converted.OrderByDescending(e => e.ConvertedAmount).First();
        var highestExpense = new ExpenseDto(
            highest.Id, highest.ConvertedAmount, highest.Description, highest.Date,
            highest.CategoryId, highest.CategoryName, highest.CategoryColor,
            displayCurrency, displayCurrencySymbol, highest.CreatedAt);

        // Category breakdown
        var categoryGroups = converted
            .GroupBy(e => new { e.CategoryName, e.CategoryColor })
            .Select(g => new { g.Key.CategoryName, g.Key.CategoryColor, Total = g.Sum(e => e.ConvertedAmount) })
            .OrderByDescending(g => g.Total)
            .ToList();

        var topCategory = categoryGroups[0].CategoryName;

        var categoryBreakdown = categoryGroups
            .Select(g => new CategorySpendingDto(
                g.CategoryName,
                g.CategoryColor,
                g.Total,
                totalSpent == 0 ? 0d : Math.Round((double)(g.Total / totalSpent) * 100, 2)))
            .ToList();

        // Daily spending totals (converted)
        var dailySpending = converted
            .GroupBy(e => e.Date.Date)
            .Select(g => new DailySpendingDto(g.Key, g.Sum(e => e.ConvertedAmount)))
            .OrderBy(d => d.Date)
            .ToList();

        return new DashboardDto(
            totalSpent,
            transactionCount,
            highestExpense,
            topCategory,
            displayCurrency,
            displayCurrencySymbol,
            categoryBreakdown,
            dailySpending);
    }
}
