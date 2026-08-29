using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FocusMapApi.Tests.Infrastructure;

public static class JwtTestHelper
{
    public const string TestKey = "focus-map-test-secret-key-at-least-256-bits-long-enough-for-hs256";
    public const string TestIssuer = "FocusMapTest";
    public const string TestAudience = "FocusMapTestAudience";

    public static string GenerateToken(Guid userId, string email, string role = "Professional")
    {
        var claims = new List<Claim>
        {
            new("UserId", userId.ToString()),
            new(ClaimTypes.Email, email),
            new("UserRole", role),
            new(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
