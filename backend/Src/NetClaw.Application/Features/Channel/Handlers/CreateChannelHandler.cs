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
        IAgentRepo agentRepo,
        IAgentTeamRepo agentTeamRepo,
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

        var targetValidation = await ValidateReplyTargetAsync(command.AgentId, command.AgentTeamId, agentRepo, agentTeamRepo, ct);
        if (targetValidation.IsFailed)
        {
            return Result.Fail<ChannelResponse>(targetValidation.Errors);
        }

        var channel = new Domains.Entities.Channel(
            name,
            command.Kind,
            "stopped",
            cryptoService.Encrypt(JsonSerializer.Serialize(new { token = command.Token.Trim() })),
            command.SettingsJson,
            command.AgentId,
            command.AgentTeamId);

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
