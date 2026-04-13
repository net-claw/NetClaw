using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

// TODO: Add Embedding (float[]) column + pgvector index for semantic retrieval in the future.
// For now, load top-K by Importance score.
[Table("UserMemories")]
public class UserMemory : AggregateRoot
{
    public UserMemory(
        Guid userId,
        string key,
        string value,
        float importance,
        string? source = null,
        string? createdBy = null)
        : base(createdBy)
    {
        UserId = userId;
        Key = key.Trim().ToLowerInvariant();
        Value = value.Trim();
        Importance = Math.Clamp(importance, 0f, 1f);
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
    }

    private UserMemory()
    {
    }

    public Guid UserId { get; private set; }

    /// <summary>Snake_case fact key, e.g. "name", "language_pref", "tech_stack".</summary>
    public string Key { get; private set; } = null!;

    public string Value { get; private set; } = null!;

    /// <summary>0.0–1.0. Used for top-K retrieval. Higher = loaded first.</summary>
    public float Importance { get; private set; }

    /// <summary>Agent id or context label that extracted this fact.</summary>
    public string? Source { get; private set; }

    public void Update(string value, float importance, string? source = null, string? updatedBy = null)
    {
        Value = value.Trim();
        Importance = Math.Clamp(importance, 0f, 1f);
        if (!string.IsNullOrWhiteSpace(source))
        {
            Source = source.Trim();
        }

        SetUpdatedBy(updatedBy ?? "System");
    }
}
