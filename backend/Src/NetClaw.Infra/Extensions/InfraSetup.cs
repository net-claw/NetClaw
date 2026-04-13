using System.Diagnostics.CodeAnalysis;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.Application.Services;
using NetClaw.Domains.Entities.Identity;
using NetClaw.EfCore.Extensions;
using NetClaw.Plugin.Abstractions;
using Npgsql;
using NetClaw.Infra.Contexts;
using NetClaw.Infra.RuntimeSkills;
using NetClaw.Infra.Services;

namespace NetClaw.Infra.Extensions;

[ExcludeFromCodeCoverage]
public static class InfraSetup
{
    #region Methods

    private static IServiceCollection AddImplementations(this IServiceCollection services)
    {
        services.Scan(s => s.FromAssemblies(typeof(InfraSetup).Assembly)
            .AddClasses(
                c => c.Where(t =>
                    t is { IsSealed: true, Namespace: not null }
                    && (t.Namespace!.Contains(".Repos", StringComparison.Ordinal)
                        || t.Namespace!.Contains(".Services", StringComparison.Ordinal))),
                false)
            .AsMatchingInterface()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddMapster(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
                !assembly.IsDynamic
                && assembly.GetName().Name?.StartsWith("NetClaw.", StringComparison.Ordinal) == true)
            .ToArray();

        config.Scan(assemblies);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    public static IServiceCollection AddInfraServices(
        this IServiceCollection service,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NetClawDb")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'NetClawDb' is missing.");

        service
            .AddDbContext<AppDbContext>(options => options.UseSqlWithMigration(connectionString))
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        service.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "netclaw_auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                },
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
            };
        });

        service.AddAuthorization();
        service.AddHttpClient();
        service.AddDataProtection();
        service.AddMapster();
        service.AddSingleton<RuntimeSkillCatalog>();
        service.AddSingleton<SkillInstallationService>();
        service.AddSingleton<ExcelSandboxService>();
        service.AddSingleton<IAgentToolService, AgentToolService>();
        service
            .AddGenericRepositories<AppDbContext>()
            .AddImplementations();

        // ChannelInboundDispatcher is injected into singleton plugin managers — must be singleton.
        service.AddSingleton<IChannelInboundDispatcher, ChannelInboundDispatcher>();

        return service;
    }

    public static async Task MigrateInfraDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = dbContext.Database.GetConnectionString()
                               ?? throw new InvalidOperationException(
                                   "Connection string 'NetClawDb' is missing.");

        await CreateDatabaseIfMissingAsync(connectionString, cancellationToken);

        var migrations = dbContext.Database.GetMigrations();
        if (migrations.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static async Task CreateDatabaseIfMissingAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var source = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(source.Database))
        {
            throw new InvalidOperationException("Database name is missing in the connection string.");
        }

        var admin = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name;",
            connection);
        existsCommand.Parameters.AddWithValue("name", source.Database);

        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
        if (exists)
        {
            return;
        }

        var quotedDatabaseName = new NpgsqlCommandBuilder().QuoteIdentifier(source.Database);
        await using var createCommand = new NpgsqlCommand(
            $"CREATE DATABASE {quotedDatabaseName};",
            connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static DbContextOptionsBuilder UseSqlWithMigration(
        this DbContextOptionsBuilder builder,
        string connectionString)
    {
        builder.ConfigureWarnings(warnings =>
        {
            // warnings.Log(RelationalEventId.PendingModelChangesWarning);
            warnings.Log(CoreEventId.ManyServiceProvidersCreatedWarning);
        });
#if DEBUG
        builder.EnableDetailedErrors().EnableSensitiveDataLogging();
#endif

        return builder.UseNpgsql(
            connectionString,
            o =>
            {
                o.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                o.EnableRetryOnFailure();
            });
        // return builder.UseSqlServer(
        //     connectionString,
        //     o => o
        //         .MinBatchSize(1)
        //         .MaxBatchSize(100)
        //         .MigrationsHistoryTable(nameof(AppDbContext), DomainSchemas.Migration)
        //         .MigrationsAssembly(typeof(AppDbContext).Assembly)
        //         .EnableRetryOnFailure()
        //         .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }

    #endregion
}
