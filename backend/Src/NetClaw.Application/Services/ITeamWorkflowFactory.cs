using NetClaw.Application.Models.Llm;

namespace NetClaw.Application.Services;

public interface ITeamWorkflowFactory
{
    TeamCompiledWorkflow Create(TeamRuntimeContext team);
}
