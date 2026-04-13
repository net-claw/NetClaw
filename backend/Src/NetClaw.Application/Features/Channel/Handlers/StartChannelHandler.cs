using FluentResults;
using NetClaw.Application.Services;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Handlers;

public sealed class StartChannelHandler
{
    public async Task<Result<ChannelResponse>> Handle(
        StartChannel command,
        IChannelService channelService,
        CancellationToken ct)
        => await channelService.StartAsync(command.ChannelId, ct);
}
