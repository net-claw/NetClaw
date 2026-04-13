using Microsoft.Extensions.DependencyInjection;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Plugin.Telegram;

public sealed class TelegramPlugin : IPlugin, IServiceConfigurablePlugin
{
    private TelegramPluginManager? _manager;

    public void ConfigureServices(IServiceCollection services, IPluginContext context)
    {
        services.AddSingleton<TelegramPluginManager>();
        services.AddSingleton<IChannelKindRuntime, TelegramChannelRuntime>();
    }

    public Task StartAsync(IPluginContext context, CancellationToken ct)
    {
        _manager = context.Services.GetRequiredService<TelegramPluginManager>();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_manager is not null)
            await _manager.StopAllAsync();
    }
}
