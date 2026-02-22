using System.ComponentModel.DataAnnotations;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Models;
using PennaiWise.Api.Services;

namespace PennaiWise.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync)
             .WithName("Register")
             .WithSummary("Register a new user")
             .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
             .WithName("Login")
             .WithSummary("Authenticate and receive a JWT")
             .AllowAnonymous();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto dto,
        IUserRepository users,
        IUnitOfWork uow,
        TokenService tokenService)
    {
        if (!new EmailAddressAttribute().IsValid(dto.Email))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "email", ["Invalid email format."] }
            });

        if (dto.Password.Length < 6)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "password", ["Password must be at least 6 characters."] }
            });

        var normalizedEmail = dto.Email.ToLowerInvariant();

        if (await users.GetByEmailAsync(normalizedEmail) is not null)
            return Results.Conflict(new { message = "Email is already registered." });

        var user = new User
        {
            Email        = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        await users.AddAsync(user);
        await uow.SaveChangesAsync();

        return Results.Ok(new AuthResponseDto(tokenService.GenerateToken(user), user.Email));
    }

    private static async Task<IResult> LoginAsync(
        LoginDto dto,
        IUserRepository users,
        TokenService tokenService)
    {
        var user = await users.GetByEmailAsync(dto.Email.ToLowerInvariant());

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Results.Unauthorized();

        return Results.Ok(new AuthResponseDto(tokenService.GenerateToken(user), user.Email));
    }
}
