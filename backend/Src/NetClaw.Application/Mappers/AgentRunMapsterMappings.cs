using Mapster;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;

namespace NetClaw.Application.Mappers;

public class AgentRunMapsterMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AgentRunStep, AgentRunStepResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.NodeId, src => src.NodeId != null ? src.NodeId.Value.ToString() : null)
            .Map(dest => dest.AgentId, src => src.AgentId != null ? src.AgentId.Value.ToString() : null)
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.CompletedAt, src => src.CompletedOn != null ? src.CompletedOn.Value.ToString("O") : null);

        config.NewConfig<AgentRun, AgentRunListItemResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.TargetId, src => src.TargetId.ToString())
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.CompletedAt, src => src.CompletedOn != null ? src.CompletedOn.Value.ToString("O") : null);

        config.NewConfig<AgentRun, AgentRunResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.TargetId, src => src.TargetId.ToString())
            .Map(dest => dest.Steps, src => src.Steps
                .OrderBy(s => s.Sequence)
                .ThenBy(s => s.CreatedOn)
                .ToList())
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.CompletedAt, src => src.CompletedOn != null ? src.CompletedOn.Value.ToString("O") : null);
    }
}
