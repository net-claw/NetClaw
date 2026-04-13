namespace NetClaw.Contracts;

public record GetAgentsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Status);

public record CreateAgentRequest(
    string Name,
    string Role,
    string Kind,
    string Type,
    string Status,
    IReadOnlyList<string> ProviderIds,
    IReadOnlyList<string> SkillIds,
    string? ModelOverride,
    string SystemPrompt,
    double? Temperature,
    int? MaxTokens,
    string? MetadataJson);

public record UpdateAgentRequest(
    string Name,
    string Role,
    string Kind,
    string Type,
    string Status,
    IReadOnlyList<string> ProviderIds,
    IReadOnlyList<string> SkillIds,
    string? ModelOverride,
    string SystemPrompt,
    double? Temperature,
    int? MaxTokens,
    string? MetadataJson);

public record AgentProviderLinkResponse(
    string ProviderId,
    string Name,
    string Provider,
    string Model,
    int Priority);

public record AgentResponse(
    string Id,
    string Name,
    string Role,
    string Kind,
    string Type,
    string Status,
    string? ModelOverride,
    string SystemPrompt,
    double? Temperature,
    int? MaxTokens,
    string? MetadataJson,
    IReadOnlyList<AgentProviderLinkResponse> Providers,
    IReadOnlyList<string> ProviderIds,
    IReadOnlyList<string> SkillIds,
    string CreatedAt,
    string? UpdatedAt);
