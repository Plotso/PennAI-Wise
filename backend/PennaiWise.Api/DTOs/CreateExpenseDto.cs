namespace PennaiWise.Api.DTOs;

public record CreateExpenseDto(
    decimal Amount,
    string Description,
    DateTime Date,
    int CategoryId,
    string CurrencyCode = "EUR"
);
