using System.Text.Json.Serialization;

namespace NetClaw.Contracts;

public record GetAgentsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Status);

public record CreateAgentRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("providerIds")] IReadOnlyList<string> ProviderIds,
    [property: JsonPropertyName("skillIds")] IReadOnlyList<string> SkillIds,
    [property: JsonPropertyName("modelOverride")] string? ModelOverride,
    [property: JsonPropertyName("systemPrompt")] string SystemPrompt,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("maxTokens")] int? MaxTokens,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson);

public record UpdateAgentRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("providerIds")] IReadOnlyList<string> ProviderIds,
    [property: JsonPropertyName("skillIds")] IReadOnlyList<string> SkillIds,
    [property: JsonPropertyName("modelOverride")] string? ModelOverride,
    [property: JsonPropertyName("systemPrompt")] string SystemPrompt,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("maxTokens")] int? MaxTokens,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson);

public record AgentProviderLinkResponse(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("priority")] int Priority);

public record AgentResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("modelOverride")] string? ModelOverride,
    [property: JsonPropertyName("systemPrompt")] string SystemPrompt,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("maxTokens")] int? MaxTokens,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("providers")] IReadOnlyList<AgentProviderLinkResponse> Providers,
    [property: JsonPropertyName("providerIds")] IReadOnlyList<string> ProviderIds,
    [property: JsonPropertyName("skillIds")] IReadOnlyList<string> SkillIds,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt);
