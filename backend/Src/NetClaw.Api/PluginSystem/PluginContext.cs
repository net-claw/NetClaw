using NetClaw.Plugin.Abstractions;

namespace NetClaw.Api.PluginSystem;

internal sealed class PluginContext(string pluginId, IServiceProvider services) : IPluginContext
{
    public string PluginId { get; } = pluginId;
    public IServiceProvider Services { get; } = services;
}
