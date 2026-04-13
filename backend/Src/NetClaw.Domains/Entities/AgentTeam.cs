using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentTeams")]
public class AgentTeam : AggregateRoot
{
    public AgentTeam(
        string name,
        string? description,
        string status,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = status.Trim().ToLowerInvariant();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private AgentTeam()
    {
    }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public string Status { get; private set; } = null!;

    public string? MetadataJson { get; private set; }

    public ICollection<AgentTeamMember> Members { get; private set; } = [];

    public void Update(
        string name,
        string? description,
        string status,
        string? metadataJson,
        string? updatedBy = null)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = status.Trim().ToLowerInvariant();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
