using FluentResults;
using NetClaw.Plugin.Abstractions;

namespace NetClaw.Plugin.Discord;

internal sealed class DiscordChannelRuntime(DiscordPluginManager manager) : IChannelKindRuntime
{
    public string Kind => "discord";

    public Task<Result> StartAsync(Guid channelId, string token, CancellationToken ct = default)
        => manager.StartAsync(channelId, token, ct);

    public Task<Result> StopAsync(Guid channelId, CancellationToken ct = default)
        => manager.StopAsync(channelId, ct);

    public Task<Result> RestartAsync(Guid channelId, string token, CancellationToken ct = default)
        => manager.RestartAsync(channelId, token, ct);
}
