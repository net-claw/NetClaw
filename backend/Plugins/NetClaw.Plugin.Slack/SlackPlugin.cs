using Microsoft.Extensions.DependencyInjection;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Plugin.Slack;

public sealed class SlackPlugin : IPlugin, IServiceConfigurablePlugin
{
    private SlackPluginManager? _manager;

    public void ConfigureServices(IServiceCollection services, IPluginContext context)
    {
        services.AddSingleton<SlackPluginManager>();
        services.AddSingleton<IChannelKindRuntime, SlackChannelRuntime>();
    }

    public Task StartAsync(IPluginContext context, CancellationToken ct)
    {
        _manager = context.Services.GetRequiredService<SlackPluginManager>();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_manager is not null)
            await _manager.StopAllAsync();
    }
}
