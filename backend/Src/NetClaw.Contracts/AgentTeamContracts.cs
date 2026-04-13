using System.Text.Json.Serialization;

namespace NetClaw.Contracts;

public record GetAgentTeamsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Status);

public record CreateAgentTeamMemberRequest(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("agentId")] string AgentId,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reportsToMemberId")] string? ReportsToMemberId,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson);

public record UpdateAgentTeamMemberRequest(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("agentId")] string AgentId,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reportsToMemberId")] string? ReportsToMemberId,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson);

public record CreateAgentTeamRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("members")] IReadOnlyList<CreateAgentTeamMemberRequest> Members);

public record UpdateAgentTeamRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("members")] IReadOnlyList<UpdateAgentTeamMemberRequest> Members);

public record AgentTeamMemberResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("agentId")] string AgentId,
    [property: JsonPropertyName("agentName")] string AgentName,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reportsToMemberId")] string? ReportsToMemberId,
    [property: JsonPropertyName("reportsToMemberName")] string? ReportsToMemberName,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt);

public record AgentTeamResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("members")] IReadOnlyList<AgentTeamMemberResponse> Members,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt);
