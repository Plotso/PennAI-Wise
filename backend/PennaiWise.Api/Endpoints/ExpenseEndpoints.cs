using PennaiWise.Api.DTOs;
using PennaiWise.Api.Extensions;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/expenses")
                       .RequireAuthorization();

        group.MapGet("/",          GetAllAsync)
             .WithName("GetExpenses")
             .WithSummary("Get paginated, filtered expenses for the current user");

        group.MapGet("/{id:int}",  GetByIdAsync)
             .WithName("GetExpense")
             .WithSummary("Get a single expense by ID");

        group.MapPost("/",         CreateAsync)
             .WithName("CreateExpense")
             .WithSummary("Create a new expense");

        group.MapPut("/{id:int}",  UpdateAsync)
             .WithName("UpdateExpense")
             .WithSummary("Update an existing expense");

        group.MapDelete("/{id:int}", DeleteAsync)
             .WithName("DeleteExpense")
             .WithSummary("Delete an expense");
    }

    // ── GET /api/expenses ────────────────────────────────────────────────────

    private static async Task<IResult> GetAllAsync(
        HttpContext http,
        IExpenseRepository expenses,
        DateTime? startDate,
        DateTime? endDate,
        int? categoryId,
        int page     = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var result = await expenses.GetUserExpensesAsync(
            userId, startDate, endDate, categoryId, page, pageSize, ct);

        return Results.Ok(result);
    }

    // ── GET /api/expenses/{id} ───────────────────────────────────────────────

    private static async Task<IResult> GetByIdAsync(
        int id,
        HttpContext http,
        IExpenseRepository expenses,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var dto = await expenses.GetByIdAsync(id, userId, ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    // ── POST /api/expenses ───────────────────────────────────────────────────

    private static async Task<IResult> CreateAsync(
        CreateExpenseDto dto,
        HttpContext http,
        IExpenseRepository expenses,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        if (ValidationError(dto.Amount, dto.Description, dto.Date) is { } err)
            return err;

        if (!await expenses.CategoryExistsForUserAsync(dto.CategoryId, userId, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "categoryId", ["Category not found or not accessible."] }
            });

        var expense = new Expense
        {
            Amount      = dto.Amount,
            Description = dto.Description.Trim(),
            Date        = dto.Date,
            CategoryId  = dto.CategoryId,
            UserId      = userId
        };

        await expenses.AddAsync(expense, ct);
        await uow.SaveChangesAsync(ct);

        var created = await expenses.GetByIdAsync(expense.Id, userId, ct);
        return Results.Created($"/api/expenses/{expense.Id}", created);
    }

    // ── PUT /api/expenses/{id} ───────────────────────────────────────────────

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateExpenseDto dto,
        HttpContext http,
        IExpenseRepository expenses,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var expense = await expenses.GetEntityByIdAsync(id, userId, ct);
        if (expense is null)
            return Results.NotFound();

        if (ValidationError(dto.Amount, dto.Description, dto.Date) is { } err)
            return err;

        if (!await expenses.CategoryExistsForUserAsync(dto.CategoryId, userId, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "categoryId", ["Category not found or not accessible."] }
            });

        expense.Amount      = dto.Amount;
        expense.Description = dto.Description.Trim();
        expense.Date        = dto.Date;
        expense.CategoryId  = dto.CategoryId;

        await uow.SaveChangesAsync(ct);

        var updated = await expenses.GetByIdAsync(id, userId, ct);
        return Results.Ok(updated);
    }

    // ── DELETE /api/expenses/{id} ────────────────────────────────────────────

    private static async Task<IResult> DeleteAsync(
        int id,
        HttpContext http,
        IExpenseRepository expenses,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var expense = await expenses.GetEntityByIdAsync(id, userId, ct);
        if (expense is null)
            return Results.NotFound();

        expenses.Remove(expense);
        await uow.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ── Shared validation ────────────────────────────────────────────────────

    private static IResult? ValidationError(decimal amount, string description, DateTime date)
    {
        var errors = new Dictionary<string, string[]>();

        if (amount <= 0)
            errors["amount"] = ["Amount must be greater than zero."];

        if (string.IsNullOrWhiteSpace(description))
            errors["description"] = ["Description is required."];

        if (date.ToUniversalTime() > DateTime.UtcNow)
            errors["date"] = ["Date cannot be in the future."];

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }
}
