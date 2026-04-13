using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentTeamMembers")]
public class AgentTeamMember : AggregateRoot
{
    public AgentTeamMember(
        Guid agentTeamId,
        Guid agentId,
        string? role,
        int order,
        string status,
        Guid? reportsToMemberId = null,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        AgentTeamId = agentTeamId;
        AgentId = agentId;
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        Order = order;
        Status = status.Trim().ToLowerInvariant();
        ReportsToMemberId = reportsToMemberId;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private AgentTeamMember()
    {
    }

    public Guid AgentTeamId { get; private set; }

    public Guid AgentId { get; private set; }

    public string? Role { get; private set; }

    public int Order { get; private set; }

    public string Status { get; private set; } = null!;

    public Guid? ReportsToMemberId { get; private set; }

    public string? MetadataJson { get; private set; }

    public AgentTeam AgentTeam { get; private set; } = null!;

    public Agent Agent { get; private set; } = null!;

    public AgentTeamMember? ReportsToMember { get; private set; }

    public ICollection<AgentTeamMember> DirectReports { get; private set; } = [];

    public void Update(
        Guid agentId,
        string? role,
        int order,
        string status,
        Guid? reportsToMemberId,
        string? metadataJson,
        string? updatedBy = null)
    {
        AgentId = agentId;
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        Order = order;
        Status = status.Trim().ToLowerInvariant();
        ReportsToMemberId = reportsToMemberId;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
