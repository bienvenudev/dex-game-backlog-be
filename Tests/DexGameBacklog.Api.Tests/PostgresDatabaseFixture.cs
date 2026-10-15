using DexGameBacklog.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Tests;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly string connectionString =
        Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING")
        ?? throw new InvalidOperationException(
            "TEST_DATABASE_CONNECTION_STRING must be set for PostgreSQL tests.");

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
