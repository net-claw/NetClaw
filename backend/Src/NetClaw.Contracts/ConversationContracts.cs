namespace NetClaw.Contracts;

public record ConversationMessageResponse(
    string Id,
    int Sequence,
    string Role,
    string? Content,
    string? ExternalMessageId,
    string? MetadataJson,
    string CreatedAt);

public record ConversationListItemResponse(
    string Id,
    string ExternalId,
    string? Title,
    string Status,
    string? TargetType,
    string? TargetId,
    string? MetadataJson,
    string LastMessageAt,
    string CreatedAt);

public record ConversationResponse(
    string Id,
    string ExternalId,
    string? Title,
    string Status,
    string? TargetType,
    string? TargetId,
    string? MetadataJson,
    string LastMessageAt,
    string CreatedAt,
    IReadOnlyList<ConversationMessageResponse> Messages);
