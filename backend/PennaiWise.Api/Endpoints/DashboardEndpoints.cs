using PennaiWise.Api.Extensions;
using PennaiWise.Api.Interfaces;

namespace PennaiWise.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", GetDashboardAsync)
           .WithName("GetDashboard")
           .WithSummary("Get aggregated spending data for a given month/year")
           .RequireAuthorization();
    }

    private static async Task<IResult> GetDashboardAsync(
        HttpContext http,
        IDashboardRepository dashboard,
        int? month,
        int? year,
        CancellationToken ct)
    {
        if (http.User.GetUserId() is not int userId)
            return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var resolvedMonth = month ?? now.Month;
        var resolvedYear  = year  ?? now.Year;

        if (resolvedMonth is < 1 or > 12)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "month", ["Month must be between 1 and 12."] }
            });

        if (resolvedYear < 2000 || resolvedYear > now.Year + 1)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "year", [$"Year must be between 2000 and {now.Year + 1}."] }
            });

        var result = await dashboard.GetDashboardAsync(userId, resolvedMonth, resolvedYear, ct);
        return Results.Ok(result);
    }
}
