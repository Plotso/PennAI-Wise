using PennaiWise.Api.DTOs;

namespace PennaiWise.Api.Interfaces;

public interface IDashboardRepository
{
    /// <summary>
    /// Returns aggregated spending data for a given month/year.
    /// All grouping and summing is performed at the DB level via IQueryable.
    /// </summary>
    Task<DashboardDto> GetDashboardAsync(int userId, int month, int year, CancellationToken ct = default);
}
