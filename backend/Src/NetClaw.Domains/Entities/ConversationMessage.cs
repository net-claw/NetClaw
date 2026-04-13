using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("ConversationMessages")]
public class ConversationMessage : AggregateRoot
{
    public ConversationMessage(
        Guid conversationId,
        int sequence,
        string role,
        string? content,
        string? externalMessageId = null,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        ConversationId = conversationId;
        Sequence = sequence;
        Role = role.Trim().ToLowerInvariant();
        Content = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        ExternalMessageId = string.IsNullOrWhiteSpace(externalMessageId) ? null : externalMessageId.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private ConversationMessage()
    {
    }

    public Guid ConversationId { get; private set; }

    public int Sequence { get; private set; }

    public string Role { get; private set; } = null!;

    public string? Content { get; private set; }

    public string? ExternalMessageId { get; private set; }

    public string? MetadataJson { get; private set; }

    public Conversation Conversation { get; private set; } = null!;
}
