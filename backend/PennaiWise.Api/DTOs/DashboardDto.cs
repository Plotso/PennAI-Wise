namespace PennaiWise.Api.DTOs;

public record DashboardDto(
    int Month,
    int Year,
    decimal TotalSpent,
    List<CategorySpendingDto> SpendingByCategory
);

public record CategorySpendingDto(
    int CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal TotalAmount,
    int ExpenseCount
);
