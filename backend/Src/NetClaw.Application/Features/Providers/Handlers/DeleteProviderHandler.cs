using FluentResults;
using NetClaw.Application.Features.Providers.Commands;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Providers.Handlers;

public sealed class DeleteProviderHandler
{
    public async Task<Result> Handle(
        DeleteProviderCommand command,
        IProviderRepo repo,
        CancellationToken ct)
    {
        var provider = await repo.FindAsync(command.ProviderId, ct);
        if (provider is null)
        {
            return Result.Ok()
                .WithSuccess(new Success("Provider not found.").WithMetadata("StatusCode", 204));
        }

        repo.Delete(provider);
        await repo.SaveChangesAsync(ct);

        return Result.Ok()
            .WithSuccess(new Success("Provider deleted.").WithMetadata("StatusCode", 200));
    }
}
