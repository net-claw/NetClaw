using System.ComponentModel.DataAnnotations.Schema;
using NetClaw.Domains.Share;

namespace NetClaw.Domains.Entities;

[Table("governanceSettings")]
public sealed class GovernanceSetting : AggregateRoot
{
    public GovernanceSetting(
        GovernanceScopeType scopeType,
        Guid? scopeId,
        bool enableBuiltinPromptInjection = true,
        bool enableCustomPromptInjection = true,
        bool enableAudit = true,
        bool enableMetrics = true,
        bool enableCircuitBreaker = false,
        string? builtinDetectorConfig = null,
        bool isActive = true,
        string? createdBy = null)
        : base(createdBy)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
        EnableBuiltinPromptInjection = enableBuiltinPromptInjection;
        EnableCustomPromptInjection = enableCustomPromptInjection;
        EnableAudit = enableAudit;
        EnableMetrics = enableMetrics;
        EnableCircuitBreaker = enableCircuitBreaker;
        BuiltinDetectorConfig = string.IsNullOrWhiteSpace(builtinDetectorConfig) ? null : builtinDetectorConfig.Trim();
        IsActive = isActive;
    }

    private GovernanceSetting()
    {
    }

    public GovernanceScopeType ScopeType { get; private set; }

    public Guid? ScopeId { get; private set; }

    public bool EnableBuiltinPromptInjection { get; private set; } = true;

    public bool EnableCustomPromptInjection { get; private set; } = true;

    public bool EnableAudit { get; private set; } = true;

    public bool EnableMetrics { get; private set; } = true;

    public bool EnableCircuitBreaker { get; private set; }

    public string? BuiltinDetectorConfig { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(
        bool enableBuiltinPromptInjection,
        bool enableCustomPromptInjection,
        bool enableAudit,
        bool enableMetrics,
        bool enableCircuitBreaker,
        string? builtinDetectorConfig,
        bool isActive,
        string? updatedBy = null)
    {
        EnableBuiltinPromptInjection = enableBuiltinPromptInjection;
        EnableCustomPromptInjection = enableCustomPromptInjection;
        EnableAudit = enableAudit;
        EnableMetrics = enableMetrics;
        EnableCircuitBreaker = enableCircuitBreaker;
        BuiltinDetectorConfig = string.IsNullOrWhiteSpace(builtinDetectorConfig) ? null : builtinDetectorConfig.Trim();
        IsActive = isActive;
        SetUpdatedBy(updatedBy ?? "System");
    }
}
