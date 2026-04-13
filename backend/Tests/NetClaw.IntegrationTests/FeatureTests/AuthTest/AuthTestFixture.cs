using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetClaw.Infra.Contexts;
using Npgsql;
using Xunit;

namespace NetClaw.IntegrationTests.FeatureTests.AuthTest;

public sealed class AuthTestFixture : IAsyncLifetime
{
    public AuthApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync();

        Factory = new AuthApplicationFactory();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    private static async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var contentRoot = Path.GetDirectoryName(typeof(Program).Assembly.Location)
                          ?? throw new InvalidOperationException("Unable to determine application path.");
        var testConfigPath = Path.Combine(contentRoot, "appsettings.test.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(testConfigPath, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("NetClawDb")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'NetClawDb' is missing.");

        // Clear local connection pool to release lingering connections before DROP
        NpgsqlConnection.ClearAllPools();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
    }
}
