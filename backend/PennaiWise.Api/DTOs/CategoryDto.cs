namespace PennaiWise.Api.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string? Color,
    int? UserId   // null = system/default category
);
