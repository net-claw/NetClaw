using FluentResults;
using NetClaw.Application.Features.Providers.Commands;
using NetClaw.Contracts.Requests.Llm;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Providers.Handlers;

public sealed class CreateProviderHandler
{
    public async Task<IResult<ProviderData>> Handle(
        CreateProviderCommand command,
        IProviderRepo repo,
        CancellationToken ct)
    {
        var name = command.Name.Trim();
        if (await repo.ExistsAsync(provider => provider.Name == name, ct))
        {
            return Result.Fail<ProviderData>(
                new Error("Provider name already exists.").WithMetadata("StatusCode", 400));
        }

        var provider = new Domains.Entities.Provider(
            name,
            command.ProviderType,
            command.DefaultModel,
            command.ApiKey,
            command.BaseUrl,
            command.IsActive);

        await repo.AddAsync(provider, ct);
        await repo.SaveChangesAsync(ct);

        return Result.Ok(provider.ToData())
            .WithSuccess(new Success("Provider created.").WithMetadata("StatusCode", 200));
    }
}
