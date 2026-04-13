using Mapster;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;

namespace NetClaw.Application.Mappers;

public class AgentMapsterMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AgentProvider, AgentProviderLinkResponse>()
            .Map(dest => dest.ProviderId, src => src.ProviderId.ToString())
            .Map(dest => dest.Name, src => src.Provider.Name)
            .Map(dest => dest.Provider, src => src.Provider.ProviderType)
            .Map(dest => dest.Model, src => src.ModelOverride ?? src.Provider.DefaultModel);

        config.NewConfig<Agent, AgentResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Providers, src => src.AgentProviders
                .OrderBy(p => p.Priority)
                .Select(p => new AgentProviderLinkResponse(
                    p.ProviderId.ToString(),
                    p.Provider.Name,
                    p.Provider.ProviderType,
                    p.ModelOverride ?? p.Provider.DefaultModel,
                    p.Priority))
                .ToList())
            .Map(dest => dest.ProviderIds, src => src.AgentProviders
                .OrderBy(p => p.Priority)
                .Select(p => p.ProviderId.ToString())
                .ToList())
            .Map(dest => dest.SkillIds, src => src.AgentSkills
                .Where(s => s.Status == "active")
                .Select(s => s.SkillId.ToString())
                .ToList())
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.UpdatedAt, src => src.UpdatedOn != null ? src.UpdatedOn.Value.ToString("O") : null);
    }
}
