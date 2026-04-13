using Microsoft.Agents.AI;

namespace NetClaw.Application.Models.Llm;

public sealed record TeamRuntimeContext(
    Guid Id,
    string Name,
    string? Description,
    string? MetadataJson,
    IReadOnlyList<TeamMemberRuntimeContext> Members);

public sealed record TeamMemberRuntimeContext(
    Guid TeamMemberId,
    Guid AgentId,
    string AgentName,
    string? Role,
    int Order,
    Guid? ReportsToMemberId,
    string SystemPrompt,
    IReadOnlyList<AgentRuntimeProvider> Providers,
    IReadOnlyList<NetClaw.Domains.Entities.Skill> Skills);

public sealed record TeamCompiledWorkflow(
    TeamMemberRuntimeContext Orchestrator,
    TeamMemberRuntimeContext Writer,
    TeamMemberRuntimeContext Reviewer,
    bool SupportsRewriteLoop);

public sealed record TeamWorkflowExecutionContext(
    TeamRuntimeContext Team,
    string OriginalRequest,
    IReadOnlyDictionary<Guid, AIAgent> Agents);

public sealed record AgentRuntimeProvider(
    Guid ProviderId,
    string DefaultModel,
    string? ModelOverride,
    string ApiKey,
    string BaseUrl);
public sealed record AgentRuntimeContext(
    Guid Id,
    string Name,
    string SystemPrompt,
    IReadOnlyList<AgentRuntimeProvider> Providers,
    IReadOnlyList<NetClaw.Domains.Entities.Skill> Skills);

// kept here after ProviderCatalog was removed
public sealed record ProviderSelection(string Provider, string Id, string Model);

public sealed record DetectionEnvelope(
    string Input,
    NetClaw.Application.Services.GovernancePromptInjectionDetection Result);
