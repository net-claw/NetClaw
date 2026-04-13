using System.Text.Json.Serialization;

namespace NetClaw.Contracts;

public record ConversationMessageResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("externalMessageId")] string? ExternalMessageId,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("createdAt")] string CreatedAt);

public record ConversationListItemResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("externalId")] string ExternalId,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("targetType")] string? TargetType,
    [property: JsonPropertyName("targetId")] string? TargetId,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("lastMessageAt")] string LastMessageAt,
    [property: JsonPropertyName("createdAt")] string CreatedAt);

public record ConversationResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("externalId")] string ExternalId,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("targetType")] string? TargetType,
    [property: JsonPropertyName("targetId")] string? TargetId,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("lastMessageAt")] string LastMessageAt,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("messages")] IReadOnlyList<ConversationMessageResponse> Messages);
