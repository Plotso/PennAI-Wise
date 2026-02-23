using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PennaiWise.Api.Data;
using PennaiWise.Api.Endpoints;
using PennaiWise.Api.Interfaces;
using PennaiWise.Api.Middleware;
using PennaiWise.Api.Repositories.Sqlite;
using PennaiWise.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Global Exception Handling ────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── HTTP Request Logging ─────────────────────────────────────────────────────
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod
                          | HttpLoggingFields.RequestPath
                          | HttpLoggingFields.ResponseStatusCode
                          | HttpLoggingFields.Duration;
    logging.CombineLogs = true;
});

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ───────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secret     = jwtSection["Secret"]   ?? throw new InvalidOperationException("JWT Secret is not configured.");
var issuer     = jwtSection["Issuer"]   ?? throw new InvalidOperationException("JWT Issuer is not configured.");
var audience   = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey  = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────────────────
// Dev policy: allow the Vite dev server on localhost.
// Production policy: origins are driven by Cors:AllowedOrigins in config.
const string DevCorsPolicy  = "ViteDev";
const string ProdCorsPolicy = "ProductionCors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());

    var productionOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    options.AddPolicy(ProdCorsPolicy, policy =>
    {
        if (productionOrigins.Length > 0)
            policy.WithOrigins(productionOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        else
            // Fallback: no credentials, deny all cross-origin — safe default.
            policy.SetIsOriginAllowed(_ => false);
    });
});

// ── OpenAPI / Swagger ────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
builder.Services.AddScoped<IUserRepository, SqliteUserRepository>();
builder.Services.AddScoped<ICategoryRepository, SqliteCategoryRepository>();
builder.Services.AddScoped<IExpenseRepository, SqliteExpenseRepository>();
builder.Services.AddScoped<IDashboardRepository, SqliteDashboardRepository>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<TokenService>();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed Default Data ────────────────────────────────────────────────────────
// Skip in the "Testing" environment: the test host owns schema creation and
// seeding via EnsureCreated() before the first request is served.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db);
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
// Exception handler must be first so it wraps everything downstream.
app.UseExceptionHandler();

app.UseHttpLogging();

// Only force HTTPS in production; in development the Kestrel HTTP endpoint is
// convenient for tools like curl without needing to trust the dev cert.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Select the appropriate CORS policy based on the hosting environment.
var corsPolicy = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")
    ? DevCorsPolicy
    : ProdCorsPolicy;
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // /openapi/v1.json
}

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithSummary("Health check");

app.MapAuthEndpoints();
app.MapCategoryEndpoints();
app.MapExpenseEndpoints();
app.MapDashboardEndpoints();

app.Run();

// Expose the implicit Program class to the test project so that
// WebApplicationFactory<Program> can reference it.
public partial class Program { }
