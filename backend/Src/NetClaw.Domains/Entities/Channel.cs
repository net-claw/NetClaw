using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("channels")]
public class Channel : AggregateRoot
{
    public Channel(
        string name,
        string kind,
        string status,
        string encryptedCredentials,
        string? settingsJson = null,
        Guid? agentId = null,
        Guid? agentTeamId = null,
        string? createdBy = null)
        : base(createdBy)
    {
        Name = name.Trim();
        Kind = kind.Trim().ToLowerInvariant();
        Status = status.Trim().ToLowerInvariant();
        EncryptedCredentials = encryptedCredentials.Trim();
        SettingsJson = string.IsNullOrWhiteSpace(settingsJson) ? null : settingsJson.Trim();
        AgentId = agentId;
        AgentTeamId = agentTeamId;
    }

    private Channel()
    {
    }

    public string Name { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string Status { get; private set; } = null!;

    public string EncryptedCredentials { get; private set; } = null!;

    public string? SettingsJson { get; private set; }

    public Guid? AgentId { get; private set; }

    public Guid? AgentTeamId { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void Update(
        string name,
        string kind,
        string status,
        string encryptedCredentials,
        string? settingsJson,
        Guid? agentId,
        Guid? agentTeamId,
        string? updatedBy = null)
    {
        Name = name.Trim();
        Kind = kind.Trim().ToLowerInvariant();
        Status = status.Trim().ToLowerInvariant();
        EncryptedCredentials = encryptedCredentials.Trim();
        SettingsJson = string.IsNullOrWhiteSpace(settingsJson) ? null : settingsJson.Trim();
        AgentId = agentId;
        AgentTeamId = agentTeamId;
        SetUpdatedBy(updatedBy ?? "System");
    }

    public void Start(string? updatedBy = null)
    {
        Status = "running";
        SetUpdatedBy(updatedBy ?? "System");
    }

    public void Stop(string? updatedBy = null)
    {
        Status = "stopped";
        SetUpdatedBy(updatedBy ?? "System");
    }

    public void Restart(string? updatedBy = null)
    {
        Status = "running";
        SetUpdatedBy(updatedBy ?? "System");
    }

    public void MarkError(string? updatedBy = null)
    {
        Status = "error";
        SetUpdatedBy(updatedBy ?? "System");
    }

    public void SoftDelete(string? updatedBy = null)
    {
        DeletedAt = DateTimeOffset.UtcNow;
        Status = "stopped";
        SetUpdatedBy(updatedBy ?? "System", DeletedAt);
    }
}
