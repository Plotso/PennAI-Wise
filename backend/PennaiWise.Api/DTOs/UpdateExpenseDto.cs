namespace PennaiWise.Api.DTOs;

public record UpdateExpenseDto(
    decimal Amount,
    string Description,
    DateTime Date,
    int CategoryId,
    string CurrencyCode = "EUR"
);
