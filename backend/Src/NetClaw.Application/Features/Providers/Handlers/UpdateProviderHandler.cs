using FluentResults;
using NetClaw.Application.Features.Providers.Commands;
using NetClaw.Contracts.Requests.Llm;
using NetClaw.Domains.Repos;

namespace NetClaw.Application.Features.Providers.Handlers;

public sealed class UpdateProviderHandler
{
    public async Task<Result<ProviderData>> Handle(
        UpdateProviderCommand command,
        IProviderRepo repo,
        CancellationToken ct)
    {
        var provider = await repo.FindAsync(command.ProviderId, ct);
        if (provider is null)
        {
            return Result.Fail<ProviderData>(
                new Error("Provider not found.").WithMetadata("StatusCode", 404));
        }

        var name = command.Name.Trim();
        var duplicate = await repo.FindAsync(item => item.Id != command.ProviderId && item.Name == name, ct);
        if (duplicate is not null)
        {
            return Result.Fail<ProviderData>(
                new Error("Provider name already exists.").WithMetadata("StatusCode", 400));
        }

        provider.Update(
            name,
            command.ProviderType,
            command.DefaultModel,
            string.IsNullOrWhiteSpace(command.ApiKey) ? provider.EncryptedApiKey : command.ApiKey,
            command.BaseUrl,
            command.IsActive);

        await repo.SaveChangesAsync(ct);

        return Result.Ok(provider.ToData())
            .WithSuccess(new Success("Provider updated.").WithMetadata("StatusCode", 200));
    }
}
