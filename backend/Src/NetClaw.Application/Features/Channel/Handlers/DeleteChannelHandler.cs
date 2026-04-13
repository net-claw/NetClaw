using FluentResults;
using NetClaw.Contracts.Channel;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Channel.Handlers;

public sealed class DeleteChannelHandler
{
    public async Task<Result> Handle(
        DeleteChannel command,
        IChannelRepo repo,
        CancellationToken ct)
    {
        var channel = await repo.FindAsync(command.ChannelId, ct);
        if (channel is null || channel.DeletedAt.HasValue)
        {
            return Result.Ok().WithSuccess(new Success("Channel not found.").WithMetadata("StatusCode", 204));
        }

        channel.SoftDelete();
        await repo.SaveChangesAsync(ct);

        return Result.Ok()
            .WithSuccess(new Success("Channel deleted.").WithMetadata("StatusCode", 200));
    }
}
