using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetClaw.Infra.Contexts;
using Npgsql;
using Reqnroll;
using NetClaw.AcceptanceTests.Support;

namespace NetClaw.AcceptanceTests.Hooks;

[Binding]
public sealed class TestHooks(ScenarioState scenarioState)
{
    [BeforeScenario]
    public async Task BeforeScenarioAsync()
    {
        await ResetDatabaseAsync();

        scenarioState.Factory = new AcceptanceApplicationFactory();
        scenarioState.Client = scenarioState.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [AfterScenario]
    public async Task AfterScenarioAsync()
    {
        scenarioState.Client.Dispose();
        await scenarioState.Factory.DisposeAsync();
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

        NpgsqlConnection.ClearAllPools();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
    }
}
