using FluentResults;
using NetClaw.Contracts;

namespace NetClaw.Application.Services;

public interface IChannelService
{
    Task<Result<ChannelResponse>> StartAsync(Guid channelId, CancellationToken cancellationToken = default);
    Task<Result<ChannelResponse>> StopAsync(Guid channelId, CancellationToken cancellationToken = default);
    Task<Result<ChannelResponse>> RestartAsync(Guid channelId, CancellationToken cancellationToken = default);
    Task RecoverRunningChannelsAsync(CancellationToken cancellationToken = default);
}
