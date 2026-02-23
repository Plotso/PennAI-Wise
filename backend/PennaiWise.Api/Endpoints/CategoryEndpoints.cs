using PennaiWise.Api.DTOs;
using PennaiWise.Api.Extensions;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories")
                       .RequireAuthorization();

        group.MapGet("/",     GetAllAsync)
             .WithName("GetCategories")
             .WithSummary("Get all categories visible to the current user");

        group.MapPost("/",    CreateAsync)
             .WithName("CreateCategory")
             .WithSummary("Create a custom category for the current user");

        group.MapDelete("/{id:int}", DeleteAsync)
             .WithName("DeleteCategory")
             .WithSummary("Delete a user-owned category, reassigning its expenses if needed");
    }

    // ── GET /api/categories ──────────────────────────────────────────────────

    private static async Task<IResult> GetAllAsync(
        HttpContext http,
        ICategoryRepository categories,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var result = await categories.GetUserCategoriesAsync(userId, ct);
        return Results.Ok(result);
    }

    // ── POST /api/categories ─────────────────────────────────────────────────

    private static async Task<IResult> CreateAsync(
        CreateCategoryDto dto,
        HttpContext http,
        ICategoryRepository categories,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        // Validate name
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "name", ["Name is required."] }
            });

        if (dto.Name.Length > 100)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "name", ["Name must be 100 characters or fewer."] }
            });

        var category = new Category
        {
            Name   = dto.Name.Trim(),
            Color  = string.IsNullOrWhiteSpace(dto.Color) ? "#808080" : dto.Color.Trim(),
            UserId = userId
        };

        await categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        var responseDto = new CategoryDto(
            category.Id,
            category.Name,
            category.Color,
            IsDefault: false);

        return Results.Created($"/api/categories/{category.Id}", responseDto);
    }

    // ── DELETE /api/categories/{id} ──────────────────────────────────────────

    private static async Task<IResult> DeleteAsync(
        int id,
        HttpContext http,
        ICategoryRepository categories,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var category = await categories.GetByIdAsync(id, ct);

        if (category is null)
            return Results.NotFound();

        // Only user-owned categories may be deleted
        if (category.UserId != userId)
            return Results.Forbid();

        // Reassign any linked expenses to the default "Other" category
        if (await categories.HasExpensesAsync(id, ct))
        {
            var otherId = await categories.GetDefaultOtherCategoryIdAsync(ct);
            if (otherId is null)
                return Results.Problem("Default 'Other' category not found; cannot reassign expenses.");

            await categories.ReassignExpensesToCategoryAsync(id, otherId.Value, ct);
        }

        categories.Remove(category);
        await uow.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
