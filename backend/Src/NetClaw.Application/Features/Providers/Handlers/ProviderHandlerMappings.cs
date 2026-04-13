using NetClaw.Contracts.Requests.Llm;

namespace NetClaw.Application.Features.Providers.Handlers;

internal static class ProviderHandlerMappings
{
    public static ProviderData ToData(this Domains.Entities.Provider provider)
        => new(
            provider.Id,
            provider.Name,
            provider.ProviderType,
            provider.DefaultModel,
            provider.BaseUrl,
            provider.IsActive,
            provider.CreatedBy,
            provider.CreatedOn,
            provider.UpdatedBy,
            provider.UpdatedOn);
}
