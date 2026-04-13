using System.Text.Json.Serialization;

namespace NetClaw.Contracts;

public record GetProvidersRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    bool? Active);

public record CreateProviderRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("apiKey")] string ApiKey,
    [property: JsonPropertyName("baseUrl")] string? BaseUrl,
    [property: JsonPropertyName("active")] bool Active);

public record UpdateProviderRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("apiKey")] string? ApiKey,
    [property: JsonPropertyName("baseUrl")] string? BaseUrl,
    [property: JsonPropertyName("active")] bool Active);

public record ProviderResponse(
    Guid Id,
    string Name,
    string ProviderType,
    string DefaultModel,
    string? BaseUrl,
    bool IsActive,
    string CreatedBy,
    DateTimeOffset CreatedOn,
    string? UpdatedBy,
    DateTimeOffset? UpdatedOn);
