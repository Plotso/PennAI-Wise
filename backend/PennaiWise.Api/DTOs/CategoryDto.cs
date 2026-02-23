namespace PennaiWise.Api.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string? Color,
    bool IsDefault   // true when UserId is null (system/default category)
);
