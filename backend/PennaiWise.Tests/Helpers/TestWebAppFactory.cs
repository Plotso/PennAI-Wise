using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PennaiWise.Api.Data;
using PennaiWise.Api.DTOs;
using PennaiWise.Api.Models;

namespace PennaiWise.Tests.Helpers;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the production
/// SQLite database with an EF Core in-memory database, seeds default categories
/// and a test user, and exposes a helper for obtaining a pre-authenticated
/// <see cref="HttpClient"/>.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    // ── Well-known test credentials ──────────────────────────────────────────
    public const string TestUserEmail    = "testuser@pennaiwise.test";
    public const string TestUserPassword = "TestPass123!";

    // One unique DB name per factory instance keeps parallel test fixtures isolated.
    private readonly string _dbName = $"PennaiWiseTests_{Guid.NewGuid():N}";

    // ── WebApplicationFactory overrides ─────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // ConfigureTestServices runs AFTER the app registers its own services,
        // so removing the SQLite descriptor here leaves only in-memory.
        builder.ConfigureTestServices(services =>
        {
            // Remove every descriptor that EF Core / the SQLite provider
            // registered so that only one database provider ends up in the
            // service collection.
            RemoveDbContextDescriptors(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureCreated();
        SeedTestData(db);

        return host;
    }

    // ── Seeding ──────────────────────────────────────────────────────────────

    private static void SeedTestData(AppDbContext db)
    {
        SeedDefaultCategories(db);
        SeedTestUser(db);
        db.SaveChanges();
    }

    private static void SeedDefaultCategories(AppDbContext db)
    {
        if (db.Categories.Any(c => c.UserId == null))
            return;

        db.Categories.AddRange(
            new Category { Name = "Food & Dining",     Color = "#FF6384" },
            new Category { Name = "Transportation",    Color = "#36A2EB" },
            new Category { Name = "Entertainment",     Color = "#FFCE56" },
            new Category { Name = "Shopping",          Color = "#4BC0C0" },
            new Category { Name = "Bills & Utilities", Color = "#9966FF" },
            new Category { Name = "Health",            Color = "#FF9F40" },
            new Category { Name = "Other",             Color = "#C9CBCF" }
        );
    }

    private static void SeedTestUser(AppDbContext db)
    {
        if (db.Users.Any(u => u.Email == TestUserEmail))
            return;

        db.Users.Add(new User
        {
            Email        = TestUserEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestUserPassword)
        });
    }

    // ── Authentication helper ────────────────────────────────────────────────

    /// <summary>
    /// Returns an <see cref="HttpClient"/> whose <c>Authorization</c> header is
    /// pre-populated with the Bearer JWT for the seeded test user.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto(TestUserEmail, TestUserPassword));

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>()
            ?? throw new InvalidOperationException(
                "Login succeeded but response could not be deserialized.");

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);

        return client;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes all service descriptors added by <c>AddDbContext&lt;AppDbContext&gt;</c>
    /// and the SQLite provider so that re-registering with a different provider
    /// doesn't trigger EF Core's "multiple providers" error.
    /// </summary>
    private static void RemoveDbContextDescriptors(IServiceCollection services)
    {
        // Types added by AddDbContext / provider registration that must be cleared.
        var typesToRemove = new[]
        {
            typeof(DbContextOptions<AppDbContext>),
            typeof(DbContextOptions),
            typeof(IDbContextOptionsConfiguration<AppDbContext>),
        };

        foreach (var type in typesToRemove)
        {
            var descriptors = services.Where(d => d.ServiceType == type).ToList();
            foreach (var d in descriptors)
                services.Remove(d);
        }
    }
}
