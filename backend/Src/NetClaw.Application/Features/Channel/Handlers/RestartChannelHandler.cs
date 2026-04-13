using FluentResults;
using NetClaw.Application.Services;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;

namespace NetClaw.Application.Features.Channel.Handlers;

public sealed class RestartChannelHandler
{
    public async Task<Result<ChannelResponse>> Handle(
        RestartChannel command,
        IChannelService channelService,
        CancellationToken ct)
        => await channelService.RestartAsync(command.ChannelId, ct);
}
