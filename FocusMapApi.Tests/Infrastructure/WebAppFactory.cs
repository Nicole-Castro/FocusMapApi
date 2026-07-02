using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FocusMapApi.Tests.Infrastructure;

public class WebAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Key"] = JwtTestHelper.TestKey,
                ["Jwt:Issuer"] = JwtTestHelper.TestIssuer,
                ["Jwt:Audience"] = JwtTestHelper.TestAudience,
                ["Cors:AllowedOrigins"] = "http://localhost:3000",
            });
        });

        // ConfigureAppConfiguration overrides config values, but Program.cs reads them into
        // local variables at startup before ConfigureAppConfiguration fires in some host models.
        // Override the JwtBearerOptions directly so token validation always uses the test key.
        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JwtTestHelper.TestIssuer,
                    ValidAudience = JwtTestHelper.TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtTestHelper.TestKey)),
                };
            });
        });
    }
}
