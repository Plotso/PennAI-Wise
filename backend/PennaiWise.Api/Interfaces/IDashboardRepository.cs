using PennaiWise.Api.DTOs;

namespace PennaiWise.Api.Interfaces;

public interface IDashboardRepository
{
    /// <summary>
    /// Returns aggregated spending data for a given month/year.
    /// Amounts are converted to <paramref name="displayCurrency"/> using the
    /// exchange rate service.
    /// </summary>
    Task<DashboardDto> GetDashboardAsync(
        int userId, int month, int year, string displayCurrency, string displayCurrencySymbol,
        CancellationToken ct = default);
}
