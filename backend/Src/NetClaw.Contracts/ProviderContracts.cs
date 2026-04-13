namespace NetClaw.Contracts;

public record GetProvidersRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    bool? Active);

public record CreateProviderRequest(
    string Name,
    string Provider,
    string Model,
    string ApiKey,
    string? BaseUrl,
    bool Active);

public record UpdateProviderRequest(
    string Name,
    string Provider,
    string Model,
    string? ApiKey,
    string? BaseUrl,
    bool Active);

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
