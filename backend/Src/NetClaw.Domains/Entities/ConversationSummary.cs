using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

/// <summary>
/// Incremental LLM-generated summary of a conversation's older messages.
/// Covers messages up to <see cref="CoveredUpToSequence"/>.
/// Messages beyond that sequence are loaded verbatim as the recent window.
/// </summary>
[Table("ConversationSummaries")]
public class ConversationSummary : AggregateRoot
{
    public ConversationSummary(
        Guid conversationId,
        string summaryText,
        int coveredUpToSequence,
        int tokenCount,
        string? createdBy = null)
        : base(createdBy)
    {
        ConversationId = conversationId;
        SummaryText = summaryText.Trim();
        CoveredUpToSequence = coveredUpToSequence;
        TokenCount = tokenCount;
    }

    private ConversationSummary()
    {
    }

    public Guid ConversationId { get; private set; }

    public string SummaryText { get; private set; } = null!;

    /// <summary>All messages with Sequence ≤ this value are captured in the summary.</summary>
    public int CoveredUpToSequence { get; private set; }

    /// <summary>Estimated token count of SummaryText (TiktokenSharp cl100k_base).</summary>
    public int TokenCount { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    public void Update(string summaryText, int coveredUpToSequence, int tokenCount, string? updatedBy = null)
    {
        SummaryText = summaryText.Trim();
        CoveredUpToSequence = coveredUpToSequence;
        TokenCount = tokenCount;
        SetUpdatedBy(updatedBy ?? "System");
    }
}
