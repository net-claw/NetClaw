using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Plugin.Sample;

/// <summary>
/// Sample plugin demonstrating all plugin interfaces:
/// - IPlugin (lifecycle)
/// - IServiceConfigurablePlugin (DI registration)
/// - IEndpointPlugin (endpoint mapping)
/// </summary>
public sealed class SamplePlugin :
    IPlugin,
    IServiceConfigurablePlugin,
    IEndpointPlugin
{
    private ILogger<SamplePlugin>? _logger;

    // --- IPlugin ---

    public Task StartAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        _logger = context.Services.GetService<ILogger<SamplePlugin>>();
        _logger?.LogInformation("[SamplePlugin] Started (pluginId={PluginId})", context.PluginId);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("[SamplePlugin] Stopped");
        return Task.CompletedTask;
    }

    // --- IServiceConfigurablePlugin ---

    public void ConfigureServices(IServiceCollection services, IPluginContext context)
    {
        services.AddSingleton<SampleGreetingService>();
    }

    // --- IEndpointPlugin ---

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IPluginContext context)
    {
        var group = endpoints.MapGroup("/plugins/sample")
            .WithTags("Sample Plugin");

        group.MapGet("/hello", (SampleGreetingService greeting) =>
            Results.Ok(new
            {
                message = greeting.Greet("World"),
                plugin = context.PluginId
            }));

        group.MapGet("/hello/{name}", (string name, SampleGreetingService greeting) =>
            Results.Ok(new
            {
                message = greeting.Greet(name),
                plugin = context.PluginId
            }));

        group.MapGet("/info", () =>
            Results.Ok(new
            {
                id = "sample.plugin",
                version = "1.0.0",
                capabilities = new[] { "endpoint", "services" }
            }));
    }
}

public sealed class SampleGreetingService
{
    public string Greet(string name) => $"Hello, {name}! Greetings from NetClaw.Plugin.Sample";
}
