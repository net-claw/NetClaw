using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentTeamEdges")]
public class AgentTeamEdge : AggregateRoot
{
    public AgentTeamEdge(
        Guid agentTeamId,
        Guid fromNodeId,
        Guid toNodeId,
        string edgeType,
        string? label,
        string? conditionExpression,
        int order,
        string status,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        AgentTeamId = agentTeamId;
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        EdgeType = edgeType.Trim().ToLowerInvariant();
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        ConditionExpression = string.IsNullOrWhiteSpace(conditionExpression) ? null : conditionExpression.Trim();
        Order = order;
        Status = status.Trim().ToLowerInvariant();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private AgentTeamEdge()
    {
    }

    public Guid AgentTeamId { get; private set; }

    public Guid FromNodeId { get; private set; }

    public Guid ToNodeId { get; private set; }

    public string EdgeType { get; private set; } = null!;

    public string? Label { get; private set; }

    public string? ConditionExpression { get; private set; }

    public int Order { get; private set; }

    public string Status { get; private set; } = null!;

    public string? MetadataJson { get; private set; }

    public AgentTeam AgentTeam { get; private set; } = null!;

    public AgentTeamNode FromNode { get; private set; } = null!;

    public AgentTeamNode ToNode { get; private set; } = null!;

    public void Update(
        Guid fromNodeId,
        Guid toNodeId,
        string edgeType,
        string? label,
        string? conditionExpression,
        int order,
        string status,
        string? metadataJson,
        string? updatedBy = null)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        EdgeType = edgeType.Trim().ToLowerInvariant();
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        ConditionExpression = string.IsNullOrWhiteSpace(conditionExpression) ? null : conditionExpression.Trim();
        Order = order;
        Status = status.Trim().ToLowerInvariant();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
