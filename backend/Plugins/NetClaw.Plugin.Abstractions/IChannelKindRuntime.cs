using FluentResults;

namespace NetClaw.Plugin.Abstractions;

public interface IChannelKindRuntime
{
    string Kind { get; }
    Task<Result> StartAsync(Guid channelId, string token, CancellationToken ct = default);
    Task<Result> StopAsync(Guid channelId, CancellationToken ct = default);
    Task<Result> RestartAsync(Guid channelId, string token, CancellationToken ct = default);
}
