using System.Text.Json.Serialization;

namespace NetClaw.Contracts;

public record GetChannelsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Kind,
    string? Status);

public record CreateChannelRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("settingsJson")] string? SettingsJson,
    [property: JsonPropertyName("startNow")] bool StartNow);

public record UpdateChannelRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("settingsJson")] string? SettingsJson);

public record ChannelResponse(
    Guid Id,
    string Name,
    string Kind,
    string Status,
    string? SettingsJson,
    bool HasCredentials,
    string CreatedBy,
    DateTimeOffset CreatedOn,
    string? UpdatedBy,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset? DeletedAt);

public record ChannelPageResponse(
    IReadOnlyList<ChannelResponse> Items,
    int PageIndex,
    int PageSize,
    int TotalItems,
    int TotalPage);
