namespace PennaiWise.Api.DTOs;

public record ExpenseDto(
    int Id,
    decimal Amount,
    string Description,
    DateTime Date,
    int CategoryId,
    string CategoryName,
    string? CategoryColor,
    string CurrencyCode,
    string CurrencySymbol,
    DateTime CreatedAt
);
