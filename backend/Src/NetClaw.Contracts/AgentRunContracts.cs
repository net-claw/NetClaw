using System.Text.Json.Serialization;

namespace NetClaw.Contracts;

public record AgentRunStepResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("nodeId")] string? NodeId,
    [property: JsonPropertyName("agentId")] string? AgentId,
    [property: JsonPropertyName("stepKey")] string StepKey,
    [property: JsonPropertyName("stepType")] string StepType,
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("inputPreview")] string? InputPreview,
    [property: JsonPropertyName("outputPreview")] string? OutputPreview,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("completedAt")] string? CompletedAt);

public record AgentRunResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("targetType")] string TargetType,
    [property: JsonPropertyName("targetId")] string TargetId,
    [property: JsonPropertyName("conversationId")] string? ConversationId,
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("inputPreview")] string? InputPreview,
    [property: JsonPropertyName("outputPreview")] string? OutputPreview,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("completedAt")] string? CompletedAt,
    [property: JsonPropertyName("steps")] IReadOnlyList<AgentRunStepResponse> Steps);

public record AgentRunListItemResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("targetType")] string TargetType,
    [property: JsonPropertyName("targetId")] string TargetId,
    [property: JsonPropertyName("conversationId")] string? ConversationId,
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("inputPreview")] string? InputPreview,
    [property: JsonPropertyName("outputPreview")] string? OutputPreview,
    [property: JsonPropertyName("metadataJson")] string? MetadataJson,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("completedAt")] string? CompletedAt);
