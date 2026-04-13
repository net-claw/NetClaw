namespace NetClaw.Application.Models.Llm;

/// <summary>
/// Context window + memory settings.
/// TODO: Migrate values to SystemConfig DB table (key-value) for runtime configurability per agent/team.
/// </summary>
public class ContextSettings
{
    /// <summary>Compact trigger: total context tokens exceeding this value triggers summarization.</summary>
    public int MaxContextTokens { get; init; } = 50_000;

    /// <summary>Number of most-recent verbatim messages always kept outside the summary.</summary>
    public int RecentMessageWindow { get; init; } = 10;

    /// <summary>Tokens reserved for the model's response — deducted from usable context budget.</summary>
    public int ResponseReserve { get; init; } = 4_096;

    /// <summary>Max UserMemory records injected per request, ordered by Importance DESC.</summary>
    public int UserMemoryTopK { get; init; } = 10;

    /// <summary>Cheap model used for summarization and memory extraction background tasks.</summary>
    public string SummaryModelId { get; init; } = "claude-haiku-4-5-20251001";

    /// <summary>Tiktoken encoding used for token counting. cl100k_base is a close approximation for Claude.</summary>
    public string TiktokenEncoding { get; init; } = "cl100k_base";
}
