using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentRunSteps")]
public class AgentRunStep : AggregateRoot
{
    public AgentRunStep(
        Guid agentRunId,
        Guid? nodeId,
        Guid? agentId,
        string stepKey,
        string stepType,
        int sequence,
        string status,
        string? inputPreview,
        string? outputPreview = null,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        AgentRunId = agentRunId;
        NodeId = nodeId;
        AgentId = agentId;
        StepKey = stepKey.Trim();
        StepType = stepType.Trim().ToLowerInvariant();
        Sequence = sequence;
        Status = status.Trim().ToLowerInvariant();
        InputPreview = string.IsNullOrWhiteSpace(inputPreview) ? null : inputPreview.Trim();
        OutputPreview = string.IsNullOrWhiteSpace(outputPreview) ? null : outputPreview.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private AgentRunStep()
    {
    }

    public Guid AgentRunId { get; private set; }

    public Guid? NodeId { get; private set; }

    public Guid? AgentId { get; private set; }

    public string StepKey { get; private set; } = null!;

    public string StepType { get; private set; } = null!;

    public int Sequence { get; private set; }

    public string Status { get; private set; } = null!;

    public string? InputPreview { get; private set; }

    public string? OutputPreview { get; private set; }

    public string? MetadataJson { get; private set; }

    public DateTimeOffset? CompletedOn { get; private set; }

    public AgentRun AgentRun { get; private set; } = null!;

    public void Complete(
        string status,
        string? outputPreview,
        string? metadataJson = null,
        string? updatedBy = null)
    {
        Status = status.Trim().ToLowerInvariant();
        OutputPreview = string.IsNullOrWhiteSpace(outputPreview) ? null : outputPreview.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? MetadataJson : metadataJson.Trim();
        CompletedOn = DateTimeOffset.UtcNow;
        SetUpdatedBy(updatedBy ?? "System", CompletedOn);
    }
}
