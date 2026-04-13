using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("Providers")]
public class Provider : AggregateRoot
{
    public Provider(
        string name,
        string providerType,
        string defaultModel,
        string encryptedApiKey,
        string? baseUrl = null,
        bool isActive = true,
        string? createdBy = null)
        : base(createdBy)
    {
        Name = name.Trim();
        ProviderType = providerType.Trim().ToLowerInvariant();
        DefaultModel = defaultModel.Trim();
        EncryptedApiKey = encryptedApiKey.Trim();
        BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim();
        IsActive = isActive;
    }

    private Provider()
    {
    }

    public string Name { get; private set; } = null!;

    public string ProviderType { get; private set; } = null!;

    public string DefaultModel { get; private set; } = null!;

    public string EncryptedApiKey { get; private set; } = null!;

    public string? BaseUrl { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<AgentProvider> AgentProviders { get; private set; } = [];

    public void Update(
        string name,
        string providerType,
        string defaultModel,
        string encryptedApiKey,
        string? baseUrl,
        bool isActive,
        string? updatedBy = null)
    {
        Name = name.Trim();
        ProviderType = providerType.Trim().ToLowerInvariant();
        DefaultModel = defaultModel.Trim();
        EncryptedApiKey = encryptedApiKey.Trim();
        BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim();
        IsActive = isActive;
        SetUpdatedBy(updatedBy ?? "System");
    }
}
