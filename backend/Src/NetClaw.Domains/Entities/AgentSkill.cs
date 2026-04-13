using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("AgentSkills")]
public class AgentSkill : AggregateRoot
{
    public AgentSkill(
        Guid agentId,
        Guid skillId,
        string status = "active",
        string? createdBy = null)
        : base(createdBy)
    {
        AgentId = agentId;
        SkillId = skillId;
        Status = status.Trim().ToLowerInvariant();
    }

    private AgentSkill()
    {
    }

    public Guid AgentId { get; private set; }

    public Guid SkillId { get; private set; }

    public string Status { get; private set; } = null!;

    public Agent Agent { get; private set; } = null!;

    public Skill Skill { get; private set; } = null!;

    public void Update(string status, string? updatedBy = null)
    {
        Status = status.Trim().ToLowerInvariant();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
