using NetClaw.Contracts.Governance;
using NetClaw.Domains.Entities;

namespace NetClaw.Application.Mappers;

public static class GovernanceMappings
{
    public static GovernanceSettingResponse ToResponse(this GovernanceSetting setting)
        => new(
            setting.Id.ToString(),
            setting.ScopeType.ToString().ToLowerInvariant(),
            setting.ScopeId?.ToString(),
            setting.EnableBuiltinPromptInjection,
            setting.EnableCustomPromptInjection,
            setting.EnableAudit,
            setting.EnableMetrics,
            setting.EnableCircuitBreaker,
            setting.BuiltinDetectorConfig,
            setting.IsActive,
            setting.CreatedOn.ToString("O"),
            setting.UpdatedOn?.ToString("O"));
}
