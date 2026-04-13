using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentRuns")]
public class AgentRun : AggregateRoot
{
    public AgentRun(
        string targetType,
        Guid targetId,
        string? conversationId,
        string? messageId,
        string status,
        string? inputPreview,
        string? outputPreview = null,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        TargetType = targetType.Trim().ToLowerInvariant();
        TargetId = targetId;
        ConversationId = string.IsNullOrWhiteSpace(conversationId) ? null : conversationId.Trim();
        MessageId = string.IsNullOrWhiteSpace(messageId) ? null : messageId.Trim();
        Status = status.Trim().ToLowerInvariant();
        InputPreview = string.IsNullOrWhiteSpace(inputPreview) ? null : inputPreview.Trim();
        OutputPreview = string.IsNullOrWhiteSpace(outputPreview) ? null : outputPreview.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private AgentRun()
    {
    }

    public string TargetType { get; private set; } = null!;

    public Guid TargetId { get; private set; }

    public string? ConversationId { get; private set; }

    public string? MessageId { get; private set; }

    public string Status { get; private set; } = null!;

    public string? InputPreview { get; private set; }

    public string? OutputPreview { get; private set; }

    public string? MetadataJson { get; private set; }

    public DateTimeOffset? CompletedOn { get; private set; }

    public ICollection<AgentRunStep> Steps { get; private set; } = [];

    public void Complete(
        string status,
        string? outputPreview,
        string? metadataJson = null,
        string? updatedBy = null)
    {
        Status = status.Trim().ToLowerInvariant();
        OutputPreview = string.IsNullOrWhiteSpace(outputPreview) ? null : outputPreview.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? MetadataJson : metadataJson.Trim();
        CompletedOn = DateTimeOffset.UtcNow;
        SetUpdatedBy(updatedBy ?? "System", CompletedOn);
    }
}
