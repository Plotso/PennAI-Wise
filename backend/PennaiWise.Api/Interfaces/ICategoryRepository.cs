using PennaiWise.Api.DTOs;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Interfaces;

public interface ICategoryRepository
{
    /// <summary>
    /// Returns the user's own categories merged with system defaults via DB-level projection.
    /// No entity materialization — memory efficient.
    /// </summary>
    Task<List<CategoryDto>> GetUserCategoriesAsync(int userId, CancellationToken ct = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    void Remove(Category category);

    Task<bool> HasExpensesAsync(int categoryId, CancellationToken ct = default);

    /// <summary>Returns the Id of the system-level "Other" category (UserId = null).</summary>
    Task<int?> GetDefaultOtherCategoryIdAsync(CancellationToken ct = default);

    /// <summary>Bulk-reassigns all expenses from one category to another at the DB level.</summary>
    Task ReassignExpensesToCategoryAsync(int fromCategoryId, int toCategoryId, CancellationToken ct = default);
}
