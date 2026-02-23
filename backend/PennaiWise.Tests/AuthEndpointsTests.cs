using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PennaiWise.Api.DTOs;
using PennaiWise.Tests.Helpers;

namespace PennaiWise.Tests;

/// <summary>
/// Integration tests for POST /api/auth/register and POST /api/auth/login.
/// One <see cref="TestWebAppFactory"/> (and therefore one isolated in-memory DB)
/// is shared across all tests in the fixture.  Each registration test uses a
/// unique e-mail address to remain independent of the others.
/// </summary>
[TestFixture]
public class AuthEndpointsTests
{
    private TestWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebAppFactory();
        _client  = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Register_WithValidData_ReturnsToken()
    {
        var dto = new RegisterDto("newuser@example.com", "SecurePass1!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be(dto.Email.ToLowerInvariant());
    }

    [Test]
    public async Task Register_WithExistingEmail_Returns409()
    {
        // The seeded test user already exists in the in-memory DB.
        var dto = new RegisterDto(TestWebAppFactory.TestUserEmail, "AnyPass99!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already registered");
    }

    [Test]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var dto = new RegisterDto("weakpass@example.com", "123"); // < 6 chars

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Results.ValidationProblem → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("password");
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var dto = new LoginDto(TestWebAppFactory.TestUserEmail, TestWebAppFactory.TestUserPassword);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be(TestWebAppFactory.TestUserEmail);
    }

    [Test]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var dto = new LoginDto(TestWebAppFactory.TestUserEmail, "WrongPassword!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_WithNonexistentEmail_Returns401()
    {
        var dto = new LoginDto("ghost@nowhere.com", "DoesNotMatter1!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
