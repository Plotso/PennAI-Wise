using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteExpenseRepository(AppDbContext context) : IExpenseRepository
{
    public async Task<PaginatedResult<ExpenseDto>> GetUserExpensesAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Date <= endDate.Value);

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExpenseDto(
                e.Id,
                e.Amount,
                e.Description,
                e.Date,
                e.CategoryId,
                e.Category.Name,
                e.Category.Color,
                e.CurrencyCode,
                e.Currency.Symbol,
                e.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedResult<ExpenseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<ExpenseDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default) =>
        context.Expenses
            .AsNoTracking()
            .Where(e => e.Id == id && e.UserId == userId)
            .Select(e => new ExpenseDto(
                e.Id,
                e.Amount,
                e.Description,
                e.Date,
                e.CategoryId,
                e.Category.Name,
                e.Category.Color,
                e.CurrencyCode,
                e.Currency.Symbol,
                e.CreatedAt))
            .FirstOrDefaultAsync(ct);

    public Task<Expense?> GetEntityByIdAsync(int id, int userId, CancellationToken ct = default) =>
        context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

    public async Task AddAsync(Expense expense, CancellationToken ct = default) =>
        await context.Expenses.AddAsync(expense, ct);

    public void Remove(Expense expense) =>
        context.Expenses.Remove(expense);

    public Task<bool> CategoryExistsForUserAsync(int categoryId, int userId, CancellationToken ct = default) =>
        context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId && (c.UserId == userId || c.UserId == null), ct);
}
