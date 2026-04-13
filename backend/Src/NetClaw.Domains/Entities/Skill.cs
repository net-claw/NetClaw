using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("Skills")]
public class Skill : AggregateRoot
{
    public Skill(
        string name,
        string slug,
        string description,
        string fileName,
        string content,
        string status,
        string? metadataJson = null,
        string? archiveFileName = null,
        string? createdBy = null)
        : base(createdBy)
    {
        Name = name.Trim();
        Slug = slug.Trim();
        Description = description.Trim();
        FileName = fileName.Trim();
        Content = content;
        Status = status.Trim().ToLowerInvariant();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        ArchiveFileName = string.IsNullOrWhiteSpace(archiveFileName) ? null : archiveFileName.Trim();
    }

    private Skill()
    {
    }

    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public string? MetadataJson { get; private set; }
    public string? ArchiveFileName { get; private set; }
    public ICollection<AgentSkill> AgentSkills { get; private set; } = [];

    public void Update(
        string name,
        string slug,
        string description,
        string fileName,
        string content,
        string status,
        string? metadataJson,
        string? archiveFileName,
        string? updatedBy = null)
    {
        Name = name.Trim();
        Slug = slug.Trim();
        Description = description.Trim();
        FileName = fileName.Trim();
        Content = content;
        Status = status.Trim().ToLowerInvariant();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        ArchiveFileName = string.IsNullOrWhiteSpace(archiveFileName) ? null : archiveFileName.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }
}
