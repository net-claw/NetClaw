using Microsoft.Extensions.AI;
using NetClaw.Application.Models.Llm;

namespace NetClaw.Application.Services;

public interface ITeamWorkflowRunner
{
    Task<ChatResponse> ExecuteAsync(
        TeamCompiledWorkflow workflow,
        TeamWorkflowExecutionContext executionContext,
        ChatOptions? options,
        CancellationToken cancellationToken = default);
}
