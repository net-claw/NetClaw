using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;
using NetClaw.EfCore.Extensions.Repos;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Repos;

internal sealed class AgentTeamRepo(AppDbContext dbContext)
    : Repository<AgentTeam>(dbContext), IAgentTeamRepo
{
}
