using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FocusMapApi.Services.Auth;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using FocusMapApi.Tests.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FocusMapApi.Tests.Unit.Auth;

public class AuthServiceTests
{
    private static AuthService CreateService(AppDbContext context)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-at-least-256-bits-long-enough-for-hs256-algorithm",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            })
            .Build();

        return new AuthService(config, context);
    }

    private static AppDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ── GenerateJwtToken ─────────────────────────────────────────────────────

    [Fact]
    public void GenerateJwtToken_ValidProfessional_ShouldContainAllExpectedClaims()
    {
        var user = UserBuilder.Professional();
        var service = CreateService(CreateInMemoryContext());

        var rawToken = service.GenerateJwtToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        parsed.Claims.Should().Contain(c => c.Type == "UserId" && c.Value == user.Id.ToString());
        parsed.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        parsed.Claims.Should().Contain(c => c.Type == "UserRole" && c.Value == "Professional");
    }

    [Fact]
    public void GenerateJwtToken_ValidPatient_ShouldContainPatientRole()
    {
        var professional = UserBuilder.Professional();
        var patient = UserBuilder.Patient(professional.Id);
        var service = CreateService(CreateInMemoryContext());

        var rawToken = service.GenerateJwtToken(patient);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        parsed.Claims.Should().Contain(c => c.Type == "UserRole" && c.Value == "Patient");
    }

    [Fact]
    public void GenerateJwtToken_ShouldExpireIn24Hours()
    {
        var user = UserBuilder.Professional();
        var service = CreateService(CreateInMemoryContext());

        var rawToken = service.GenerateJwtToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        parsed.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateJwtToken_ShouldBeValidatableWithCorrectKey()
    {
        var user = UserBuilder.Professional();
        var service = CreateService(CreateInMemoryContext());

        var rawToken = service.GenerateJwtToken(user);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("test-secret-key-at-least-256-bits-long-enough-for-hs256-algorithm")),
        };

        var act = () => new JwtSecurityTokenHandler().ValidateToken(rawToken, validationParams, out _);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateJwtToken_WithNonUserModelObject_ShouldThrowArgumentException()
    {
        var service = CreateService(CreateInMemoryContext());

        var act = () => service.GenerateJwtToken("not a UserModel");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tipo de usuário inválido*");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturnBadRequest()
    {
        var service = CreateService(CreateInMemoryContext());

        var result = await service.Login(new LoginDto { email = "", password = "x" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WithWhitespaceEmail_ShouldReturnBadRequest()
    {
        var service = CreateService(CreateInMemoryContext());

        var result = await service.Login(new LoginDto { email = "   ", password = "x" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnUnauthorized()
    {
        var service = CreateService(CreateInMemoryContext());

        var result = await service.Login(new LoginDto { email = "nobody@test.com", password = "x" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithExistingEmail_ShouldReturnTokenAndUserData()
    {
        var context = CreateInMemoryContext();
        var user = UserBuilder.Professional();
        context.Profiles.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.Login(new LoginDto { email = user.Email, password = UserBuilder.DefaultPassword });

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        var context = CreateInMemoryContext();
        var user = UserBuilder.Professional();
        context.Profiles.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.Login(new LoginDto { email = user.Email, password = "wrong-password" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithNoPasswordHashSet_ShouldReturnUnauthorized()
    {
        // Conta antiga, criada antes da coluna de senha existir.
        var context = CreateInMemoryContext();
        var user = UserBuilder.Professional();
        user.PasswordHash = null;
        context.Profiles.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.Login(new LoginDto { email = user.Email, password = "qualquer-coisa" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_EmailLookup_ShouldBeCaseInsensitive()
    {
        var context = CreateInMemoryContext();
        var user = UserBuilder.Professional();
        user.Email = "Doctor@Test.com";
        context.Profiles.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.Login(new LoginDto { email = "doctor@test.com", password = UserBuilder.DefaultPassword });

        result.Success.Should().BeTrue();
    }

    // ── GoogleLogin ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleLogin_WithUnknownEmail_ShouldReturnNotFound()
    {
        var service = CreateService(CreateInMemoryContext());

        var result = await service.GoogleLogin(new GoogleLoginDto
        {
            Email = "ghost@google.com",
            Name = "Ghost",
        });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GoogleLogin_WithExistingEmail_ShouldReturnToken()
    {
        var context = CreateInMemoryContext();
        var user = UserBuilder.Professional();
        context.Profiles.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GoogleLogin(new GoogleLoginDto
        {
            Email = user.Email,
            Name = user.Name,
        });

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data.Should().NotBeNull();
    }
}
