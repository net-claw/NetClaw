using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;
using NetClaw.EfCore.Extensions.Repos;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Repos;

internal sealed class AgentSkillRepo(AppDbContext dbContext)
    : Repository<AgentSkill>(dbContext), IAgentSkillRepo
{
}
