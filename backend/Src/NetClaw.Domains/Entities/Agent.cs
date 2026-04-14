using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("Agents")]
public class Agent : AggregateRoot
{
    public Agent(
        string name,
        string? role,
        string kind,
        string? type,
        string status,
        string? systemPrompt,
        string? modelOverride = null,
        double? temperature = null,
        int? maxTokens = null,
        string? metadataJson = null,
        string? createdBy = null)
        : base(createdBy)
    {
        Name = name.Trim();
        Role = NormalizeOptionalText(role);
        Kind = kind.Trim().ToLowerInvariant();
        Type = NormalizeOptionalText(type);
        Status = status.Trim().ToLowerInvariant();
        SystemPrompt = NormalizeOptionalText(systemPrompt);
        ModelOverride = string.IsNullOrWhiteSpace(modelOverride) ? null : modelOverride.Trim();
        Temperature = temperature;
        MaxTokens = maxTokens;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
    }

    private Agent()
    {
    }

    public string Name { get; private set; } = null!;

    public string Role { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string Type { get; private set; } = null!;

    public string Status { get; private set; } = null!;

    public string SystemPrompt { get; private set; } = null!;

    public string? ModelOverride { get; private set; }

    public double? Temperature { get; private set; }

    public int? MaxTokens { get; private set; }

    public string? MetadataJson { get; private set; }

    public ICollection<AgentProvider> AgentProviders { get; private set; } = [];

    public ICollection<AgentSkill> AgentSkills { get; private set; } = [];

    public ICollection<AgentTeamMember> AgentTeamMembers { get; private set; } = [];

    public void Update(
        string name,
        string? role,
        string kind,
        string? type,
        string status,
        string? systemPrompt,
        string? modelOverride,
        double? temperature,
        int? maxTokens,
        string? metadataJson,
        string? updatedBy = null)
    {
        Name = name.Trim();
        Role = NormalizeOptionalText(role);
        Kind = kind.Trim().ToLowerInvariant();
        Type = NormalizeOptionalText(type);
        Status = status.Trim().ToLowerInvariant();
        SystemPrompt = NormalizeOptionalText(systemPrompt);
        ModelOverride = string.IsNullOrWhiteSpace(modelOverride) ? null : modelOverride.Trim();
        Temperature = temperature;
        MaxTokens = maxTokens;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        SetUpdatedBy(updatedBy ?? "System");
    }

    private static string NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
