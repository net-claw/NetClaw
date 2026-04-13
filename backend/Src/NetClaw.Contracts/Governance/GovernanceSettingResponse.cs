namespace NetClaw.Contracts.Governance;

public record GovernanceSettingResponse(
    string Id,
    string ScopeType,
    string? ScopeId,
    bool EnableBuiltinPromptInjection,
    bool EnableCustomPromptInjection,
    bool EnableAudit,
    bool EnableMetrics,
    bool EnableCircuitBreaker,
    string? BuiltinDetectorConfig,
    bool IsActive,
    string CreatedOn,
    string? UpdatedOn);
