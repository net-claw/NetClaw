using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentTeamNodes")]
public class AgentTeamNode : AggregateRoot
{
    public AgentTeamNode(
        Guid agentTeamId,
        string nodeKey,
        string name,
        string nodeType,
        Guid? agentId,
        string kind,
        string? role,
        int order,
        decimal? positionX,
        decimal? positionY,
        string status,
        string? configJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        AgentTeamId = agentTeamId;
        NodeKey = nodeKey.Trim();
        Name = name.Trim();
        NodeType = nodeType.Trim().ToLowerInvariant();
        AgentId = agentId;
        Kind = kind.Trim().ToLowerInvariant();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        Order = order;
        PositionX = positionX;
        PositionY = positionY;
        Status = status.Trim().ToLowerInvariant();
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? null : configJson.Trim();
    }

    private AgentTeamNode()
    {
    }

    public Guid AgentTeamId { get; private set; }

    public string NodeKey { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NodeType { get; private set; } = null!;

    public Guid? AgentId { get; private set; }

    public string Kind { get; private set; } = null!;

    public string? Role { get; private set; }

    public int Order { get; private set; }

    public decimal? PositionX { get; private set; }

    public decimal? PositionY { get; private set; }

    public string Status { get; private set; } = null!;

    public string? ConfigJson { get; private set; }

    public AgentTeam AgentTeam { get; private set; } = null!;

    public Agent? Agent { get; private set; }

    public ICollection<AgentTeamEdge> OutgoingEdges { get; private set; } = [];

    public ICollection<AgentTeamEdge> IncomingEdges { get; private set; } = [];

    public void Update(
        string nodeKey,
        string name,
        string nodeType,
        Guid? agentId,
        string kind,
        string? role,
        int order,
        decimal? positionX,
        decimal? positionY,
        string status,
        string? configJson,
        string? updatedBy = null)
    {
        NodeKey = nodeKey.Trim();
        Name = name.Trim();
        NodeType = nodeType.Trim().ToLowerInvariant();
        AgentId = agentId;
        Kind = kind.Trim().ToLowerInvariant();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        Order = order;
        PositionX = positionX;
        PositionY = positionY;
        Status = status.Trim().ToLowerInvariant();
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? null : configJson.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
