using FluentResults;
using NetClaw.Application.Services;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Handlers;

public sealed class StopChannelHandler
{
    public async Task<Result<ChannelResponse>> Handle(
        StopChannel command,
        IChannelService channelService,
        CancellationToken ct)
        => await channelService.StopAsync(command.ChannelId, ct);
}
