using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("Conversations")]
public class Conversation : AggregateRoot
{
    public Conversation(
        string externalId,
        string? title,
        string status,
        string? targetType = null,
        Guid? targetId = null,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        ExternalId = externalId.Trim();
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        Status = status.Trim().ToLowerInvariant();
        TargetType = string.IsNullOrWhiteSpace(targetType) ? null : targetType.Trim().ToLowerInvariant();
        TargetId = targetId;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        LastMessageOn = CreatedOn;
    }

    private Conversation()
    {
    }

    public string ExternalId { get; private set; } = null!;

    public string? Title { get; private set; }

    public string Status { get; private set; } = null!;

    public string? TargetType { get; private set; }

    public Guid? TargetId { get; private set; }

    public string? MetadataJson { get; private set; }

    public DateTimeOffset LastMessageOn { get; private set; }

    public ICollection<ConversationMessage> Messages { get; private set; } = [];

    public void Touch(
        string? title = null,
        string? status = null,
        string? metadataJson = null,
        DateTimeOffset? lastMessageOn = null,
        string? updatedBy = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            Status = status.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            MetadataJson = metadataJson.Trim();
        }

        LastMessageOn = lastMessageOn ?? DateTimeOffset.UtcNow;
        SetUpdatedBy(updatedBy ?? "System", LastMessageOn);
    }
}
