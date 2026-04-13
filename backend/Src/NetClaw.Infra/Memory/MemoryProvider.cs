using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Models.Llm;
using NetClaw.Application.Services;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Memory;

public sealed class MemoryProvider(
    ContextSettings settings,
    ILlmClientFactory llmClientFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<MemoryProvider> logger) : IMemoryProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<string?> ProvideAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var memories = await db.UserMemories
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Importance)
            .Take(settings.UserMemoryTopK)
            .ToListAsync(cancellationToken);

        if (memories.Count == 0)
        {
            return null;
        }

        return string.Join("\n", memories.Select(m => $"- {m.Key}: {m.Value}"));
    }

    /// <inheritdoc />
    public async Task StoreAsync(
        Guid userId,
        string userMessage,
        string assistantMessage,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        var facts = await ExtractFactsAsync(userMessage, assistantMessage, cancellationToken);
        if (facts.Count == 0)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var fact in facts)
        {
            if (string.IsNullOrWhiteSpace(fact.Key) || string.IsNullOrWhiteSpace(fact.Value))
            {
                continue;
            }

            var existing = await db.UserMemories
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Key == fact.Key, cancellationToken);

            if (existing is not null)
            {
                existing.Update(fact.Value, fact.Importance, source);
            }
            else
            {
                await db.UserMemories.AddAsync(
                    new UserMemory(userId, fact.Key, fact.Value, fact.Importance, source),
                    cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<MemoryFact>> ExtractFactsAsync(
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = llmClientFactory.Create(settings.SummaryModelId);

            var prompt = $$"""
                Extract durable facts about the user from this conversation turn.
                Return ONLY a JSON array, no explanation: [{"key":"snake_case","value":"string","importance":0.0}]
                Rules:
                - Only explicit, reusable facts (name, language_pref, tech_stack, location, preferences, etc.)
                - importance: 0.9=critical identity, 0.7=strong preference, 0.5=minor/contextual
                - Return [] if no facts found

                User: {{userMessage}}
                Assistant: {{assistantMessage}}
                """;

            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, prompt)],
                new ChatOptions { MaxOutputTokens = 300 },
                cancellationToken);

            var json = response.Text?.Trim() ?? "[]";

            // Strip markdown code fences if model wraps in ```json
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                var start = json.IndexOf('[');
                var end = json.LastIndexOf(']');
                json = start >= 0 && end > start ? json[start..(end + 1)] : "[]";
            }

            return JsonSerializer.Deserialize<List<MemoryFact>>(json, JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Memory extraction failed — skipping for this turn");
            return [];
        }
    }

    private sealed record MemoryFact(string Key, string Value, float Importance);
}
