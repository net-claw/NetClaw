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
        IAgentRepo agentRepo,
        IAgentTeamRepo agentTeamRepo,
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

        var targetValidation = await ValidateReplyTargetAsync(command.AgentId, command.AgentTeamId, agentRepo, agentTeamRepo, ct);
        if (targetValidation.IsFailed)
        {
            return Result.Fail<ChannelResponse>(targetValidation.Errors);
        }

        var encryptedCredentials = string.IsNullOrWhiteSpace(command.Token)
            ? channel.EncryptedCredentials
            : cryptoService.Encrypt(JsonSerializer.Serialize(new { token = command.Token.Trim() }));

        channel.Update(
            name,
            command.Kind,
            channel.Status,
            encryptedCredentials,
            command.SettingsJson,
            command.AgentId,
            command.AgentTeamId);

        await repo.SaveChangesAsync(ct);

        return Result.Ok(mapper.Map<ChannelResponse>(channel))
            .WithSuccess(new Success("Channel updated.").WithMetadata("StatusCode", 200));
    }

    private static async Task<Result> ValidateReplyTargetAsync(
        Guid? agentId,
        Guid? agentTeamId,
        IAgentRepo agentRepo,
        IAgentTeamRepo agentTeamRepo,
        CancellationToken ct)
    {
        if (agentId.HasValue == agentTeamId.HasValue)
        {
            return Result.Fail(new Error("Channel must be linked to exactly one agent or agent team.")
                .WithMetadata("StatusCode", 400));
        }

        if (agentId.HasValue)
        {
            var agent = await agentRepo.FindAsync(agentId.Value, ct);
            return agent is null
                ? Result.Fail(new Error("Agent not found.").WithMetadata("StatusCode", 400))
                : Result.Ok();
        }

        var team = await agentTeamRepo.FindAsync(agentTeamId!.Value, ct);
        return team is null
            ? Result.Fail(new Error("Agent team not found.").WithMetadata("StatusCode", 400))
            : Result.Ok();
    }
}
