using DotNet.Testcontainers.Configurations;
using FocusMapApi.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FocusMapApi.Tests.Infrastructure;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresContainerFixture()
    {
        // Ryuk (resource reaper) requires ghcr.io/testcontainers/ryuk which may not be
        // accessible in all environments. Disabling it is safe for local dev runs.
        TestcontainersSettings.ResourceReaperEnabled = false;

        _container = new PostgreSqlBuilder()
            .WithDatabase("focusmap_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() =>
        await _container.DisposeAsync();

    public async Task CleanupAsync(AppDbContext context)
    {
        await context.SessionData.ExecuteDeleteAsync();
        await context.AudioDescriptions.ExecuteDeleteAsync();
        await context.Sessions.ExecuteDeleteAsync();
        await context.InterestPoints.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Profiles.IgnoreQueryFilters()
            .Where(u => u.ProfessionalId != null)
            .ExecuteDeleteAsync();
        await context.Profiles.IgnoreQueryFilters()
            .Where(u => u.ProfessionalId == null)
            .ExecuteDeleteAsync();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgresContainerFixture> { }
