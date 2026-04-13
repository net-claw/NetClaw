namespace NetClaw.Contracts;

public record GetAgentTeamsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Status);

public record CreateAgentTeamMemberRequest(
    string? Id,
    string AgentId,
    string? Role,
    int Order,
    string Status,
    string? ReportsToMemberId,
    string? MetadataJson);

public record UpdateAgentTeamMemberRequest(
    string? Id,
    string AgentId,
    string? Role,
    int Order,
    string Status,
    string? ReportsToMemberId,
    string? MetadataJson);

public record CreateAgentTeamRequest(
    string Name,
    string? Description,
    string Status,
    string? MetadataJson,
    IReadOnlyList<CreateAgentTeamMemberRequest> Members);

public record UpdateAgentTeamRequest(
    string Name,
    string? Description,
    string Status,
    string? MetadataJson,
    IReadOnlyList<UpdateAgentTeamMemberRequest> Members);

public record AgentTeamMemberResponse(
    string Id,
    string AgentId,
    string AgentName,
    string? Role,
    int Order,
    string Status,
    string? ReportsToMemberId,
    string? ReportsToMemberName,
    string? MetadataJson,
    string CreatedAt,
    string? UpdatedAt);

public record AgentTeamResponse(
    string Id,
    string Name,
    string? Description,
    string Status,
    string? MetadataJson,
    IReadOnlyList<AgentTeamMemberResponse> Members,
    string CreatedAt,
    string? UpdatedAt);
