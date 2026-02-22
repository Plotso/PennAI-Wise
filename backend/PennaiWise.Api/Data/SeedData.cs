using Microsoft.EntityFrameworkCore;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Data;

public static class SeedData
{
    private static readonly Category[] DefaultCategories =
    [
        new() { Name = "Food & Dining",    Color = "#FF6384" },
        new() { Name = "Transportation",   Color = "#36A2EB" },
        new() { Name = "Entertainment",    Color = "#FFCE56" },
        new() { Name = "Shopping",         Color = "#4BC0C0" },
        new() { Name = "Bills & Utilities", Color = "#9966FF" },
        new() { Name = "Health",           Color = "#FF9F40" },
        new() { Name = "Other",            Color = "#C9CBCF" },
    ];

    public static async Task SeedAsync(AppDbContext db)
    {
        var existingNames = await db.Categories
            .Where(c => c.UserId == null)
            .Select(c => c.Name)
            .ToHashSetAsync();

        var toAdd = DefaultCategories
            .Where(c => !existingNames.Contains(c.Name))
            .ToList();

        if (toAdd.Count == 0)
            return;

        db.Categories.AddRange(toAdd);
        await db.SaveChangesAsync();
    }
}
