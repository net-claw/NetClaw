namespace NetClaw.Contracts.Governance;

public record UpdateGovernanceSettingRequest(
    bool EnableBuiltinPromptInjection,
    bool EnableCustomPromptInjection,
    bool EnableAudit,
    bool EnableMetrics,
    bool EnableCircuitBreaker,
    string? BuiltinDetectorConfig,
    bool IsActive);
