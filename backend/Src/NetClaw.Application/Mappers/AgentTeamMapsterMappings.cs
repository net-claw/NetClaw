using Mapster;
using NetClaw.Contracts;
using NetClaw.Domains.Entities;

namespace NetClaw.Application.Mappers;

public class AgentTeamMapsterMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AgentTeamMember, AgentTeamMemberResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.AgentId, src => src.AgentId.ToString())
            .Map(dest => dest.AgentName, src => src.Agent.Name)
            .Map(dest => dest.ReportsToMemberId, src => src.ReportsToMemberId != null ? src.ReportsToMemberId.Value.ToString() : null)
            .Map(dest => dest.ReportsToMemberName, src => src.ReportsToMember != null ? src.ReportsToMember.Agent.Name : null)
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.UpdatedAt, src => src.UpdatedOn != null ? src.UpdatedOn.Value.ToString("O") : null);

        config.NewConfig<AgentTeam, AgentTeamResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Members, src => src.Members
                .OrderBy(m => m.Order)
                .ThenBy(m => m.CreatedOn)
                .ToList())
            .Map(dest => dest.CreatedAt, src => src.CreatedOn.ToString("O"))
            .Map(dest => dest.UpdatedAt, src => src.UpdatedOn != null ? src.UpdatedOn.Value.ToString("O") : null);
    }
}
