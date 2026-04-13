using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentProviders")]
public class AgentProvider : AggregateRoot
{
    public AgentProvider(
        Guid agentId,
        Guid providerId,
        int priority = 0,
        string status = "active",
        string? modelOverride = null,
        string? createdBy = null)
        : base(createdBy)
    {
        AgentId = agentId;
        ProviderId = providerId;
        Priority = priority;
        Status = status.Trim().ToLowerInvariant();
        ModelOverride = string.IsNullOrWhiteSpace(modelOverride) ? null : modelOverride.Trim();
    }

    private AgentProvider()
    {
    }

    public Guid AgentId { get; private set; }

    public Guid ProviderId { get; private set; }

    public int Priority { get; private set; }

    public string Status { get; private set; } = null!;

    public string? ModelOverride { get; private set; }

    public Agent Agent { get; private set; } = null!;

    public Provider Provider { get; private set; } = null!;

    public void Update(
        int priority,
        string status,
        string? modelOverride,
        string? updatedBy = null)
    {
        Priority = priority;
        Status = status.Trim().ToLowerInvariant();
        ModelOverride = string.IsNullOrWhiteSpace(modelOverride) ? null : modelOverride.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
