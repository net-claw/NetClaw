
namespace NetClaw.Contracts.Requests.Llm;

public sealed record ProviderError(string Code, string Message);

public sealed record ProviderData(
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

public sealed record ProviderOperationResult(
    bool Succeeded,
    int StatusCode,
    string Message,
    ProviderData? Data = null,
    IReadOnlyDictionary<string, IReadOnlyList<ProviderError>>? Details = null)
{
    public static ProviderOperationResult Success(string message, ProviderData? data = null, int statusCode = 200)
        => new(true, statusCode, message, data);

    public static ProviderOperationResult Failure(
        int statusCode,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<ProviderError>>? details = null)
        => new(false, statusCode, message, null, details);
}
