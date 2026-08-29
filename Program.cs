using System.Text;
using AspNetCoreRateLimit;
using FocusMapApi.Data;
using FocusMapApi.Services.AudioDescription;
using FocusMapApi.Services.Auth;
using FocusMapApi.Services.InterestPoints;
using FocusMapApi.Services.OpenAi;
using FocusMapApi.Services.SessionData;
using FocusMapApi.Services.Sessions;
using FocusMapApi.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder
    .Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: false
    )
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
}

builder.WebHost.UseUrls("http://*:8080;");

// builder.WebHost.UseUrls("http://localhost:5000;");

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FocusMap API", Version = "v1" });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Insira o token JWT no formato: Bearer {seu_token_aqui}",
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});
builder
    .Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context
                .ModelState.Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new
                {
                    Field = e.Key,
                    Errors = e.Value!.Errors.Select(x => x.ErrorMessage),
                });

            return new BadRequestObjectResult(
                new
                {
                    Success = false,
                    Message = "Dados inválidos.",
                    StatusCode = 400,
                    Errors = errors,
                }
            );
        };
    });
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/Auth/login",
            Period = "5m",
            Limit = 10,
        },
        // Chamadas à OpenAI custam dinheiro por requisição: limite mais apertado que o
        // geral. O app faz ~2 chamadas a cada 10s por sessão ativa (transcrição + análise),
        // ou seja, 12/min por dispositivo. Com até 3 dispositivos atrás do mesmo IP/NAT
        // rodando sessão ao mesmo tempo, 60/min dá folga (3 * 12 = 36) sem deixar um loop
        // indevido rodar solto.
        new RateLimitRule
        {
            Endpoint = "POST:/api/OpenAI/analyze",
            Period = "1m",
            Limit = 60,
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/OpenAI/transcribe",
            Period = "1m",
            Limit = 60,
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100,
        },
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
var key = builder.Configuration["Jwt:Key"] ?? "super-secret-key";
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
builder.Services.AddHttpClient<OpenAiService>(client =>
{
    // Evita conexões penduradas segurando capacidade caso a OpenAI demore a responder.
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IOpenAiUsageGuard, OpenAiUsageGuard>();

// Provedor de transcrição é escolhido por config (OpenAI:TranscribeProvider: "openai" | "groq").
// As duas implementações continuam existindo lado a lado — trocar de volta é só mudar essa
// config e reiniciar a API, sem precisar mexer em código.
var transcribeProvider = builder.Configuration["OpenAI:TranscribeProvider"] ?? "openai";
if (string.Equals(transcribeProvider, "groq", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<GroqTranscriptionService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddScoped<ITranscriptionService, GroqTranscriptionService>();
}
else
{
    builder.Services.AddHttpClient<OpenAiTranscriptionService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddScoped<ITranscriptionService, OpenAiTranscriptionService>();
}

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthInterface, AuthService>();
builder.Services.AddScoped<IInterestPointsService, InterestPointsService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISessionDataService, SessionDataService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IAudioDescriptionService, AudioDescriptionService>();
builder.Services.AddHostedService<SessionAutoCloseService>();

var AllowFrontend =
    builder
        .Configuration["Cors:AllowedOrigins"]
        ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins(AllowFrontend).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    );
});

builder
    .Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);
var app = builder.Build();

// var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
// app.Urls.Add($"http://*:{port}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FocusMap API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler(appBuilder =>
    {
        appBuilder.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                new
                {
                    Success = false,
                    Message = "Ocorreu um erro interno. Tente novamente mais tarde.",
                    StatusCode = 500,
                }
            );
        });
    });
}

app.MapGet("/", () => "'_'");
app.UseIpRateLimiting();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.Run();

public partial class Program { }
