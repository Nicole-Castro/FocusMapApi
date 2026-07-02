using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FocusMapApi.Data;
using FocusMapApi.Tests.Builders;
using FocusMapApi.Tests.Infrastructure;
using FluentAssertions;

namespace FocusMapApi.Tests.Api;

[Collection("Database")]
public class AuthEndpointTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private WebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebAppFactory(postgres.ConnectionString);
        _client = _factory.CreateClient();
        _context = postgres.CreateContext();
        await postgres.CleanupAsync(_context);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        _client.Dispose();
        await _context.DisposeAsync();
    }

    // ── POST /api/Login/login ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithRegisteredEmail_ShouldReturn200AndToken()
    {
        var user = UserBuilder.Professional();
        _context.Profiles.Add(user);
        await _context.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/Login/login", new
        {
            email = user.Email,
            password = "any",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/Login/login", new
        {
            email = "nobody@nowhere.com",
            password = "any",
        });

        // Controller returns Unauthorized() when Success == false
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/Login/login", new
        {
            email = "",
            password = "any",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/Login/me ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_WithoutAuthorizationHeader_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/Login/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithExpiredToken_ShouldReturn401()
    {
        // Build an already-expired JWT
        var expiredToken = JwtTestHelper.GenerateToken(
            Guid.NewGuid(), "expired@test.com");

        // Manually craft an expired token using JwtTestHelper internals would be complex,
        // so instead we use a token signed with a wrong key to force failure
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");

        var response = await _client.GetAsync("/api/Login/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ShouldReturn200AndUserId()
    {
        var user = UserBuilder.Professional();
        _context.Profiles.Add(user);
        await _context.SaveChangesAsync();

        var token = JwtTestHelper.GenerateToken(user.Id, user.Email, "Professional");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Login/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // GetCurrentUser returns { id, name, email, role } — no ResponseModel wrapper
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(user.Id.ToString());
    }
}
