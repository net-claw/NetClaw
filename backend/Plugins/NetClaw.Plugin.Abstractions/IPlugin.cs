using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace NetClaw.Plugin.Abstractions;


public interface IPlugin
{
    Task StartAsync(IPluginContext context, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

public interface IServiceConfigurablePlugin
{
    void ConfigureServices(IServiceCollection services, IPluginContext context);
}

public interface IEndpointPlugin
{
    void MapEndpoints(IEndpointRouteBuilder endpoints, IPluginContext context);
}

public interface IPluginContext
{
    string PluginId { get; }
    IServiceProvider Services { get; }
}