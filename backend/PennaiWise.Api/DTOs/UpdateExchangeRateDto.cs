namespace PennaiWise.Api.DTOs;

public record UpdateExchangeRateDto(
    decimal Rate,
    DateTime EffectiveDate
);
