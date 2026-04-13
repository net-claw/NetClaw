using Asp.Versioning;
using Asp.Versioning.Builder;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.AI;
using NetClaw.Api;
using NetClaw.Api.Configs;
using NetClaw.Api.PluginSystem;
using NetClaw.Application.Features.Auth.Commands;
using NetClaw.Docker.Extensions;
using NetClaw.Infra.Extensions;
using Wolverine;
using NetClaw.Application;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Options;
using NetClaw.Application.Services;
using NetClaw.Infra.Memory;
using NetClaw.Infra.Identity;
using NetClaw.Infra.Services;
using Scalar.AspNetCore;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- Plugin system ---
var pluginsDir = Path.Combine(builder.Environment.ContentRootPath, "plugins");
var pluginLoader = new PluginLoader();
pluginLoader.LoadAll(pluginsDir);
pluginLoader.ConfigureServices(builder.Services);
builder.Services.AddSingleton(pluginLoader);

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(LoginCommand).Assembly, includeInternalTypes: true);



builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(ApplicationSetup).Assembly);
});

builder.Services
    .AddInfraServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();
builder.Services.AddDockerServices(builder.Configuration);
builder.Services.AddSingleton(new ContextSettings());
builder.Services.AddSingleton<ITokenCounter, TokenCounter>();
builder.Services.AddSingleton<ILlmClientFactory, LlmClientFactory>();
builder.Services.AddSingleton<IMemoryProvider, MemoryProvider>();
builder.Services.AddSingleton<IContextCompactor, ContextCompactor>();
builder.Services.AddSingleton<ChatModeCatalog>();
builder.Services.AddSingleton<IGovernanceService, GovernanceService>();
builder.Services.AddSingleton<ITeamWorkflowFactory, TeamWorkflowFactory>();
builder.Services.AddSingleton<ITeamWorkflowRunner, TeamWorkflowRunner>();
builder.Services.AddScoped<ITeamAgentOrchestrationService, TeamAgentOrchestrationService>();
builder.Services.AddScoped<IChatClient, ProviderRoutingChatClient>();
builder.Services.Configure<AppConfigOptions>(builder.Configuration.GetSection("AppConfigOptions"));
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

var app = builder.Build();

await app.Services.MigrateInfraDatabaseAsync();
await app.Services.SeedIdentityAsync( builder.Configuration);

var ensureSandboxOnStartup = builder.Configuration.GetValue("SANDBOX_ENSURE_ON_STARTUP", false);
if (ensureSandboxOnStartup)
{
    await app.Services.GetRequiredService<SandboxManager>().EnsureAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
    app.MapScalarApiReference("/scalar", options =>
    {
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();
RouteGroupBuilder versionedApiGroup = app
    .MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);
app.MapEndpoints(versionedApiGroup);
pluginLoader.MapEndpoints(app, app.Services);
app.MapFallbackToFile("index.html");

await pluginLoader.StartAllAsync(app.Services);
app.Run();

static class IdentityErrorExtensions
{
    public static IDictionary<string, IReadOnlyList<ApiFieldError>> ToIdentityErrorDetails(
        this IEnumerable<IdentityError> errors)
        => errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApiFieldError>)group
                    .Select(error => new ApiFieldError(error.Code, error.Description))
                    .ToList());
}
