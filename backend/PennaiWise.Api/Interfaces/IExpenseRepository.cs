using PennaiWise.Api.DTOs;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Interfaces;

public interface IExpenseRepository
{
    /// <summary>
    /// Paginated, filtered expense list for a user.
    /// Projected to DTOs via IQueryable — no entity materialization, AsNoTracking.
    /// </summary>
    Task<PaginatedResult<ExpenseDto>> GetUserExpensesAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Returns an expense DTO for display. Returns null if not found or not owned by userId.</summary>
    Task<ExpenseDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the tracked entity for update/delete operations.
    /// Returns null if not found or not owned by userId.
    /// </summary>
    Task<Expense?> GetEntityByIdAsync(int id, int userId, CancellationToken ct = default);

    Task AddAsync(Expense expense, CancellationToken ct = default);
    void Remove(Expense expense);

    /// <summary>Confirms a category is accessible to the user (own or system category).</summary>
    Task<bool> CategoryExistsForUserAsync(int categoryId, int userId, CancellationToken ct = default);
}
