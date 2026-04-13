using Microsoft.Extensions.DependencyInjection;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Plugin.Discord;

public sealed class DiscordPlugin : IPlugin, IServiceConfigurablePlugin
{
    private DiscordPluginManager? _manager;

    public void ConfigureServices(IServiceCollection services, IPluginContext context)
    {
        services.AddSingleton<DiscordPluginManager>();
        services.AddSingleton<IChannelKindRuntime, DiscordChannelRuntime>();
    }

    public Task StartAsync(IPluginContext context, CancellationToken ct)
    {
        _manager = context.Services.GetRequiredService<DiscordPluginManager>();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_manager is not null)
            await _manager.StopAllAsync();
    }
}
