using System.Text.Json;
using FluentResults;
using MapsterMapper;
using NetClaw.Application.Services;
using NetClaw.Contracts;
using NetClaw.Contracts.Channel;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Channel.Handlers;

public sealed class UpdateChannelHandler
{
    public async Task<Result<ChannelResponse>> Handle(
        UpdateChannel command,
        IChannelRepo repo,
        ISecretCryptoService cryptoService,
        IMapper mapper,
        CancellationToken ct)
    {
        var channel = await repo.FindAsync(command.ChannelId, ct);
        if (channel is null || channel.DeletedAt.HasValue)
        {
            return Result.Fail<ChannelResponse>(
                new Error("Channel not found.").WithMetadata("StatusCode", 404));
        }

        var name = command.Name.Trim();
        var duplicate = await repo.FindAsync(item => item.Id != command.ChannelId && item.Name == name && item.DeletedAt == null, ct);
        if (duplicate is not null)
        {
            return Result.Fail<ChannelResponse>(
                new Error("Channel name already exists.").WithMetadata("StatusCode", 400));
        }

        var encryptedCredentials = string.IsNullOrWhiteSpace(command.Token)
            ? channel.EncryptedCredentials
            : cryptoService.Encrypt(JsonSerializer.Serialize(new { token = command.Token.Trim() }));

        channel.Update(
            name,
            command.Kind,
            channel.Status,
            encryptedCredentials,
            command.SettingsJson);

        await repo.SaveChangesAsync(ct);

        return Result.Ok(mapper.Map<ChannelResponse>(channel))
            .WithSuccess(new Success("Channel updated.").WithMetadata("StatusCode", 200));
    }
}
