using FluentResults;
using System.Text.Json;
using MapsterMapper;
using NetClaw.Application.Services;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Channel.Handlers;

public sealed class CreateChannelHandler
{
    public async Task<Result<ChannelResponse>> Handle(
        CreateChannel command,
        IChannelRepo repo,
        ISecretCryptoService cryptoService,
        IChannelService channelService,
        IMapper mapper,
        CancellationToken ct)
    {
        var name = command.Name.Trim();
        if (await repo.ExistsAsync(channel => channel.Name == name && channel.DeletedAt == null, ct))
        {
            return Result.Fail<ChannelResponse>(
                new Error("Channel name already exists.").WithMetadata("StatusCode", 400));
        }

        var channel = new Domains.Entities.Channel(
            name,
            command.Kind,
            "stopped",
            cryptoService.Encrypt(JsonSerializer.Serialize(new { token = command.Token.Trim() })),
            command.SettingsJson);

        await repo.AddAsync(channel, ct);
        await repo.SaveChangesAsync(ct);

        if (!command.StartNow)
        {
            return Result.Ok(mapper.Map<ChannelResponse>(channel))
                .WithSuccess(new Success("Channel created.").WithMetadata("StatusCode", 200));
        }

        var start = await channelService.StartAsync(channel.Id, ct);
        return start.IsFailed
            ? Result.Fail<ChannelResponse>(start.Errors)
            : start.WithSuccess(new Success("Channel created and started.").WithMetadata("StatusCode", 200));
    }
}
