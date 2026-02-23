using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Repositories.Sqlite;

public class SqliteCategoryRepository(AppDbContext context) : ICategoryRepository
{
    public Task<List<CategoryDto>> GetUserCategoriesAsync(int userId, CancellationToken ct = default) =>
        context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId || c.UserId == null)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Color, c.UserId == null))
            .ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await context.Categories.AddAsync(category, ct);

    public void Remove(Category category) =>
        context.Categories.Remove(category);

    public Task<bool> HasExpensesAsync(int categoryId, CancellationToken ct = default) =>
        context.Expenses.AnyAsync(e => e.CategoryId == categoryId, ct);

    public Task<int?> GetDefaultOtherCategoryIdAsync(CancellationToken ct = default) =>
        context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == null && c.Name == "Other")
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);

    public Task ReassignExpensesToCategoryAsync(int fromCategoryId, int toCategoryId, CancellationToken ct = default) =>
        context.Expenses
            .Where(e => e.CategoryId == fromCategoryId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CategoryId, toCategoryId), ct);
}
