using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Memory;

public sealed class ContextCompactor(
    ContextSettings settings,
    ITokenCounter tokenCounter,
    IMemoryProvider memoryProvider,
    ILlmClientFactory llmClientFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<ContextCompactor> logger) : IContextCompactor
{
    /// <inheritdoc />
    public async Task<CompactedContext> BuildContextAsync(
        string conversationId,
        Guid? userId,
        IReadOnlyList<ChatMessage> allMessages,
        CancellationToken cancellationToken = default)
    {
        var summaryText = await LoadSummaryAsync(conversationId, cancellationToken);
        var userMemoryText = userId.HasValue
            ? await memoryProvider.ProvideAsync(userId.Value, cancellationToken)
            : null;

        // Non-system messages for the recent window
        var nonSystem = allMessages.Where(m => m.Role != ChatRole.System).ToList();
        var recentMessages = nonSystem.TakeLast(settings.RecentMessageWindow).ToList();

        var budget = settings.MaxContextTokens - settings.ResponseReserve;
        var summaryTokens = tokenCounter.Count(summaryText ?? string.Empty);
        var recentTokens = tokenCounter.Count(recentMessages);
        var memoryTokens = tokenCounter.Count(userMemoryText ?? string.Empty);
        var total = summaryTokens + recentTokens + memoryTokens;

        // Step 1: trim summary if over budget
        if (total > budget && !string.IsNullOrEmpty(summaryText))
        {
            var summaryBudget = Math.Max(200, budget - recentTokens - memoryTokens);
            summaryText = TrimToTokenBudget(summaryText, summaryBudget);
            summaryTokens = tokenCounter.Count(summaryText);
            total = summaryTokens + recentTokens + memoryTokens;
        }

        // Step 2: reduce window if still over budget
        if (total > budget)
        {
            var messageBudget = budget - summaryTokens - memoryTokens;
            recentMessages = TrimMessagesToBudget(recentMessages, messageBudget);
            total = summaryTokens + tokenCounter.Count(recentMessages) + memoryTokens;
        }

        logger.LogDebug(
            "Context built for {ConversationId}: ~{Tokens} tokens (summary={S}, recent={R}, memory={M})",
            conversationId, total, summaryTokens, tokenCounter.Count(recentMessages), memoryTokens);

        return new CompactedContext(summaryText, recentMessages, userMemoryText, total);
    }

    /// <inheritdoc />
    public async Task MaybeUpdateSummaryAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var conversation = await db.Conversations
                .AsNoTracking()
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.ExternalId == conversationId, cancellationToken);

            if (conversation is null || conversation.Messages.Count == 0)
            {
                return;
            }

            var ordered = conversation.Messages
                .OrderBy(m => m.Sequence)
                .ToList();

            var existingSummary = await db.ConversationSummaries
                .FirstOrDefaultAsync(s => s.ConversationId == conversation.Id, cancellationToken);

            var coveredUpTo = existingSummary?.CoveredUpToSequence ?? 0;

            // Messages not yet covered by summary
            var uncovered = ordered.Where(m => m.Sequence > coveredUpTo).ToList();

            // Determine window boundary — messages inside the recent window stay verbatim
            var windowBoundary = ordered.Count > settings.RecentMessageWindow
                ? ordered[^settings.RecentMessageWindow].Sequence
                : int.MaxValue;

            // Only summarize messages that are both uncovered AND outside the window
            var summarizable = uncovered
                .Where(m => m.Sequence < windowBoundary)
                .ToList();

            if (summarizable.Count == 0)
            {
                return;
            }

            // Check if total tokens actually exceed the threshold
            var summaryTokens = tokenCounter.Count(existingSummary?.SummaryText ?? string.Empty);
            var uncoveredTokens = tokenCounter.Count(
                uncovered.Select(m => new ChatMessage(ParseRole(m.Role), m.Content ?? string.Empty)));

            if (summaryTokens + uncoveredTokens <= settings.MaxContextTokens)
            {
                return;
            }

            logger.LogInformation(
                "Compacting conversation {ConversationId}: summarizing {Count} messages",
                conversationId, summarizable.Count);

            var newSummaryText = await SummarizeAsync(
                existingSummary?.SummaryText,
                summarizable,
                cancellationToken);

            var newTokenCount = tokenCounter.Count(newSummaryText);
            var newCoveredUpTo = summarizable.Max(m => m.Sequence);

            if (existingSummary is null)
            {
                db.ConversationSummaries.Add(
                    new ConversationSummary(conversation.Id, newSummaryText, newCoveredUpTo, newTokenCount));
            }
            else
            {
                existingSummary.Update(newSummaryText, newCoveredUpTo, newTokenCount);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Summary update failed for {ConversationId}", conversationId);
        }
    }

    private async Task<string?> LoadSummaryAsync(
        string conversationId,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = await db.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ExternalId == conversationId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var summary = await db.ConversationSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ConversationId == conversation.Id, cancellationToken);

        return summary?.SummaryText;
    }

    private async Task<string> SummarizeAsync(
        string? previousSummary,
        List<ConversationMessage> messages,
        CancellationToken cancellationToken)
    {
        var messagesText = string.Join("\n", messages.Select(m =>
            $"{m.Role.ToUpperInvariant()}: {m.Content}"));

        var prompt = string.IsNullOrWhiteSpace(previousSummary)
            ? $"""
               Compress this conversation history into a concise summary.
               Preserve key facts, decisions, context, and any code/technical details.
               Output only the summary, no preamble.

               MESSAGES:
               {messagesText}
               """
            : $"""
               Update this conversation summary by incorporating the new messages.
               Be concise but preserve all key facts, decisions, context, and technical details.
               Output only the updated summary, no preamble.

               EXISTING SUMMARY:
               {previousSummary}

               NEW MESSAGES TO INCORPORATE:
               {messagesText}
               """;

        var client = llmClientFactory.Create(settings.SummaryModelId);
        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            new ChatOptions { MaxOutputTokens = 1024 },
            cancellationToken);

        return response.Text?.Trim() ?? messagesText;
    }

    private string TrimToTokenBudget(string text, int budget)
    {
        if (tokenCounter.Count(text) <= budget)
        {
            return text;
        }

        // Binary-search for the longest prefix that fits
        var low = 0;
        var high = text.Length;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (tokenCounter.Count(text[..mid]) <= budget)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low > 0 ? text[..low] + "…" : string.Empty;
    }

    private List<ChatMessage> TrimMessagesToBudget(List<ChatMessage> messages, int budget)
    {
        // Keep as many tail messages as fit within budget
        var result = new List<ChatMessage>();
        var tokens = 0;
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var t = tokenCounter.Count(messages[i].Text ?? string.Empty) + 4;
            if (tokens + t > budget)
            {
                break;
            }

            result.Insert(0, messages[i]);
            tokens += t;
        }

        return result;
    }

    private static ChatRole ParseRole(string role) =>
        role.Trim().ToLowerInvariant() switch
        {
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
}
