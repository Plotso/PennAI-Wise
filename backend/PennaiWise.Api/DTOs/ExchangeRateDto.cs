namespace PennaiWise.Api.DTOs;

public record ExchangeRateDto(
    int Id,
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Rate,
    DateTime EffectiveDate
);
