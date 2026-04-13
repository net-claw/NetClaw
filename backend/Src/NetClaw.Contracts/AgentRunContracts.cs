namespace NetClaw.Contracts;

public record AgentRunStepResponse(
    string Id,
    string? NodeId,
    string? AgentId,
    string StepKey,
    string StepType,
    int Sequence,
    string Status,
    string? InputPreview,
    string? OutputPreview,
    string? MetadataJson,
    string CreatedAt,
    string? CompletedAt);

public record AgentRunResponse(
    string Id,
    string TargetType,
    string TargetId,
    string? ConversationId,
    string? MessageId,
    string Status,
    string? InputPreview,
    string? OutputPreview,
    string? MetadataJson,
    string CreatedAt,
    string? CompletedAt,
    IReadOnlyList<AgentRunStepResponse> Steps);

public record AgentRunListItemResponse(
    string Id,
    string TargetType,
    string TargetId,
    string? ConversationId,
    string? MessageId,
    string Status,
    string? InputPreview,
    string? OutputPreview,
    string? MetadataJson,
    string CreatedAt,
    string? CompletedAt);
