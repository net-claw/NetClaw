namespace NetClaw.Application.Services;

/// <summary>
/// Follows the Microsoft AIContextProvider lifecycle pattern:
///   ProvideAsync  — called BEFORE agent run to inject user memories into context.
///   StoreAsync    — called AFTER response (background) to extract + persist new facts.
/// </summary>
public interface IMemoryProvider
{
    /// <summary>Returns top-K user memories as a formatted context string, or null if none.</summary>
    Task<string?> ProvideAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts durable facts from a completed conversation turn (via haiku) and upserts them.
    /// Designed to run in the background — does not block the main response.
    /// </summary>
    Task StoreAsync(
        Guid userId,
        string userMessage,
        string assistantMessage,
        string? source = null,
        CancellationToken cancellationToken = default);
}
