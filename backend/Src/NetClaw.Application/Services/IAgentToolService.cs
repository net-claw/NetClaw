using Microsoft.Extensions.AI;

namespace NetClaw.Application.Services;

public interface IAgentToolService
{
    IReadOnlyList<AITool> GetTools();
}
