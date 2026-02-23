namespace PennaiWise.Api.DTOs;

public record DashboardDto(
    decimal TotalSpent,
    int TransactionCount,
    ExpenseDto? HighestExpense,
    string? TopCategory,
    List<CategorySpendingDto> CategoryBreakdown,
    List<DailySpendingDto> DailySpending
);

public record CategorySpendingDto(
    string CategoryName,
    string? Color,
    decimal Total,
    double Percentage
);

public record DailySpendingDto(
    DateTime Date,
    decimal Total
);
