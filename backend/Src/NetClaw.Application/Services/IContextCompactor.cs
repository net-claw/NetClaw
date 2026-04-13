using Microsoft.Extensions.AI;

namespace NetClaw.Application.Services;

/// <param name="SummaryText">Compressed summary of messages older than the recent window, null if none yet.</param>
/// <param name="RecentMessages">Last N verbatim messages (system messages excluded).</param>
/// <param name="UserMemoryText">Top-K user facts formatted as context string, null if user unknown.</param>
/// <param name="EstimatedTokens">Total estimated tokens for logging / observability.</param>
public record CompactedContext(
    string? SummaryText,
    IReadOnlyList<ChatMessage> RecentMessages,
    string? UserMemoryText,
    int EstimatedTokens);

public interface IContextCompactor
{
    /// <summary>
    /// Builds the compacted context payload for a request.
    /// Loads summary + user memories from DB, applies token budget enforcement.
    /// Does NOT make LLM calls — summarization happens in MaybeUpdateSummaryAsync.
    /// </summary>
    Task<CompactedContext> BuildContextAsync(
        string conversationId,
        Guid? userId,
        IReadOnlyList<ChatMessage> allMessages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Background: checks if total conversation tokens exceed MaxContextTokens.
    /// If so, incrementally summarizes messages outside the recent window using haiku.
    /// Safe to call fire-and-forget.
    /// </summary>
    Task MaybeUpdateSummaryAsync(
        string conversationId,
        CancellationToken cancellationToken = default);
}
