namespace PennaiWise.Api.DTOs;

public record CreateExchangeRateDto(
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Rate,
    DateTime EffectiveDate
);
