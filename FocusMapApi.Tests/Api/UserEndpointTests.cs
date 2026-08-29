using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FocusMapApi.Data;
using FocusMapApi.Tests.Builders;
using FocusMapApi.Tests.Infrastructure;
using FluentAssertions;

namespace FocusMapApi.Tests.Api;

[Collection("Database")]
public class UserEndpointTests(PostgresContainerFixture postgres) : IAsyncLifetime
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

    private void AuthorizeAs(Guid userId, string email, string role = "Professional") =>
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", JwtTestHelper.GenerateToken(userId, email, role));

    // ── POST /api/User/CreateUser (admin-only, cadastra Professional) ─────────

    [Fact]
    public async Task CreateUser_WithoutToken_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/User/CreateUser", new
        {
            email = "newpro@test.com",
            name = "New Professional",
            password = "Test@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_AsNonAdmin_ShouldReturn403()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email, "Professional");

        var response = await _client.PostAsJsonAsync("/api/User/CreateUser", new
        {
            email = "newpro2@test.com",
            name = "New Professional",
            password = "Test@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_WithValidData_ShouldReturn200()
    {
        var admin = UserBuilder.Admin();
        _context.Profiles.Add(admin);
        await _context.SaveChangesAsync();

        AuthorizeAs(admin.Id, admin.Email, "Admin");

        var response = await _client.PostAsJsonAsync("/api/User/CreateUser", new
        {
            email = "newpro3@test.com",
            name = "New Professional",
            password = "Test@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_AsAdmin_WithDuplicateEmail_ShouldReturn200WithSuccessFalse()
    {
        var admin = UserBuilder.Admin();
        _context.Profiles.Add(admin);
        await _context.SaveChangesAsync();

        AuthorizeAs(admin.Id, admin.Email, "Admin");

        await _client.PostAsJsonAsync("/api/User/CreateUser", new
        {
            email = "dup@test.com",
            name = "First",
            password = "Test@1234",
        });

        var response = await _client.PostAsJsonAsync("/api/User/CreateUser", new
        {
            email = "dup@test.com",
            name = "Second",
            password = "Test@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ── Endpoints protegidos – JWT obrigatório ────────────────────────────────

    [Fact]
    public async Task ListPatients_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/User/ListPatients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPatients_WithValidToken_ShouldReturn200()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email);

        var response = await _client.GetAsync("/api/User/ListPatients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TotalPatients_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/User/TotalPatients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/User/CreatePatient (requer token) ───────────────────────────

    [Fact]
    public async Task CreatePatient_WithoutToken_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/User/CreatePatient", new
        {
            name = "Patient",
            email = "p@test.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePatient_WithValidToken_ShouldReturn200()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email);

        var response = await _client.PostAsJsonAsync("/api/User/CreatePatient", new
        {
            name = "Patient Test",
            email = "patient-api@test.com",
            password = "Test@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ── Admin: gestão de contas Professional ──────────────────────────────────

    [Fact]
    public async Task ListProfessionals_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/User/ListProfessionals");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListProfessionals_AsNonAdmin_ShouldReturn403()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email, "Professional");

        var response = await _client.GetAsync("/api/User/ListProfessionals");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListProfessionals_AsAdmin_ShouldReturn200()
    {
        var admin = UserBuilder.Admin();
        var pro = UserBuilder.Professional();
        _context.Profiles.AddRange(admin, pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(admin.Id, admin.Email, "Admin");

        var response = await _client.GetAsync("/api/User/ListProfessionals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteProfessional_AsNonAdmin_ShouldReturn403()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email, "Professional");

        var response = await _client.DeleteAsync($"/api/User/DeleteProfessional/{pro.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProfessional_AsAdmin_ShouldReturn200()
    {
        var admin = UserBuilder.Admin();
        var pro = UserBuilder.Professional();
        _context.Profiles.AddRange(admin, pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(admin.Id, admin.Email, "Admin");

        var response = await _client.DeleteAsync($"/api/User/DeleteProfessional/{pro.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetProfessionalPassword_AsNonAdmin_ShouldReturn403()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email, "Professional");

        var response = await _client.PostAsync($"/api/User/ResetProfessionalPassword/{pro.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResetProfessionalPassword_AsAdmin_ShouldReturnNewPassword()
    {
        var admin = UserBuilder.Admin();
        var pro = UserBuilder.Professional();
        _context.Profiles.AddRange(admin, pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(admin.Id, admin.Email, "Admin");

        var response = await _client.PostAsync($"/api/User/ResetProfessionalPassword/{pro.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetString().Should().NotBeNullOrEmpty();
    }

    // ── UpdateUser: dono ou Admin ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_AsSelf_ShouldReturn200()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email, "Professional");

        var response = await _client.PatchAsync($"/api/User/UpdateUser/{pro.Id}",
            JsonContent.Create(new { name = "Novo Nome" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_OnAnotherAccount_AsNonAdmin_ShouldReturn403()
    {
        var pro1 = UserBuilder.Professional();
        var pro2 = UserBuilder.Professional();
        _context.Profiles.AddRange(pro1, pro2);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro1.Id, pro1.Email, "Professional");

        var response = await _client.PatchAsync($"/api/User/UpdateUser/{pro2.Id}",
            JsonContent.Create(new { name = "Hackeado" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUser_OnAnotherAccount_AsAdmin_ShouldReturn200()
    {
        var admin = UserBuilder.Admin();
        var pro = UserBuilder.Professional();
        _context.Profiles.AddRange(admin, pro);
        await _context.SaveChangesAsync();

        AuthorizeAs(admin.Id, admin.Email, "Admin");

        var response = await _client.PatchAsync($"/api/User/UpdateUser/{pro.Id}",
            JsonContent.Create(new { name = "Renomeado pelo admin" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── DELETE /api/User/DeletePatient/{id} ───────────────────────────────────

    [Fact]
    public async Task DeletePatient_WithoutToken_ShouldReturn401()
    {
        var response = await _client.DeleteAsync($"/api/User/DeletePatient/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePatient_WithValidTokenAndOwnPatient_ShouldReturn200()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        AuthorizeAs(pro.Id, pro.Email);

        var response = await _client.DeleteAsync($"/api/User/DeletePatient/{patient.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
