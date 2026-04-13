using AgentGovernance;
using AgentGovernance.Audit;
using AgentGovernance.Policy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Services;
using NetClaw.Domains.Entities;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.Services;

public sealed class GovernanceService : IGovernanceService, IDisposable
{
    private const string DefaultAgentDid = "did:mesh:netclaw-chat";
    private const string SettingsCacheKey = "governance:settings:all";

    private readonly GovernanceKernel _kernelWithInjection;
    private readonly GovernanceKernel _kernelWithoutInjection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GovernanceService> _logger;

    public GovernanceService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache memoryCache,
        IServiceScopeFactory scopeFactory,
        ILogger<GovernanceService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Keep the YAML policy path for diagnostics and later rollout, but do not
        // apply it yet. For now governance should be driven by the global DB setting
        // only. Re-enable PolicyPaths after the global-first flow is finalized.
        // PolicyPath = Path.Combine(environment.ContentRootPath, "governance", "default.yaml");
        _kernelWithInjection = new GovernanceKernel(new GovernanceOptions
        {
            ConflictStrategy = ConflictResolutionStrategy.DenyOverrides,
            EnableAudit = true,
            EnableMetrics = true,
            EnablePromptInjectionDetection = true,
            EnableCircuitBreaker = true,
        });
        _kernelWithoutInjection = new GovernanceKernel(new GovernanceOptions
        {
            ConflictStrategy = ConflictResolutionStrategy.DenyOverrides,
            EnableAudit = true,
            EnableMetrics = true,
            EnablePromptInjectionDetection = false,
            EnableCircuitBreaker = true,
        });

        _kernelWithInjection.OnAllEvents(HandleGovernanceEvent);
        _kernelWithoutInjection.OnAllEvents(HandleGovernanceEvent);
    }

    public string PolicyPath { get; }

    public GovernanceToolEvaluation EvaluateToolCall(string toolName, Dictionary<string, object>? args = null)
    {
        var settings = ResolveSettings();
        var kernel = settings.EnableBuiltinPromptInjection ? _kernelWithInjection : _kernelWithoutInjection;
        var result = kernel.EvaluateToolCall(ResolveAgentDid(), toolName, args ?? new Dictionary<string, object>());
        return new GovernanceToolEvaluation(
            result.Allowed,
            result.Reason,
            result.PolicyDecision?.Action,
            result.PolicyDecision?.MatchedRule);
    }

    public void InvalidateSettingsCache() => _memoryCache.Remove(SettingsCacheKey);

    public GovernancePromptInjectionDetection? DetectPromptInjection(string? input)
    {
        var settings = ResolveSettings();
        if (!settings.EnableBuiltinPromptInjection)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(input) || _kernelWithInjection.InjectionDetector is null)
        {
            return null;
        }

        var result = _kernelWithInjection.InjectionDetector.Detect(input);
        return new GovernancePromptInjectionDetection(
            result.IsInjection,
            result.InjectionType.ToString(),
            result.ThreatLevel.ToString(),
            result.Confidence,
            result.Explanation);
    }

    public void Dispose()
    {
        _kernelWithInjection.Dispose();
        _kernelWithoutInjection.Dispose();
    }

    private void HandleGovernanceEvent(GovernanceEvent evt)
    {
        var reason = evt.Data.TryGetValue("reason", out var value) ? value : null;
        _logger.LogInformation(
            "Governance event {Type} agent={AgentId} policy={PolicyName} reason={Reason}",
            evt.Type,
            evt.AgentId,
            evt.PolicyName,
            reason);
    }

    private string ResolveAgentDid()
    {
        var requestedAgentId = _httpContextAccessor.HttpContext?.Request.Query["agent_id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(requestedAgentId))
        {
            return ToDid(requestedAgentId);
        }

        var userName = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(userName))
        {
            return ToDid($"user-{userName}");
        }

        return DefaultAgentDid;
    }

    private ResolvedGovernanceSettings ResolveSettings()
    {
        var allSettings = _memoryCache.GetOrCreate(
            SettingsCacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return dbContext.GovernanceSettings
                    .AsNoTracking()
                    .Where(setting => setting.IsActive)
                    .OrderByDescending(setting => setting.CreatedOn)
                    .ToList();
            }) ?? [];

        var agentId = ParseGuid(_httpContextAccessor.HttpContext?.Request.Query["agent_id"].FirstOrDefault());
        var tenantId = ParseGuid(_httpContextAccessor.HttpContext?.Request.Query["tenant_id"].FirstOrDefault());

        var matched = allSettings.FirstOrDefault(setting =>
                          setting.ScopeType == GovernanceScopeType.Agent && setting.ScopeId == agentId)
                      ?? allSettings.FirstOrDefault(setting =>
                          setting.ScopeType == GovernanceScopeType.Tenant && setting.ScopeId == tenantId)
                      ?? allSettings.FirstOrDefault(setting =>
                          setting.ScopeType == GovernanceScopeType.Global && setting.ScopeId is null);

        return matched is null
            ? ResolvedGovernanceSettings.Default
            : new ResolvedGovernanceSettings(
                matched.EnableBuiltinPromptInjection,
                matched.EnableCustomPromptInjection,
                matched.EnableAudit,
                matched.EnableMetrics,
                matched.EnableCircuitBreaker);
    }

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;

    private static string ToDid(string rawValue)
    {
        var normalized = rawValue.Trim();
        if (normalized.StartsWith("did:", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        normalized = normalized
            .Replace(" ", "-", StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal)
            .Replace("\\", "-", StringComparison.Ordinal);

        return $"did:mesh:{normalized}";
    }
}

internal sealed record ResolvedGovernanceSettings(
    bool EnableBuiltinPromptInjection,
    bool EnableCustomPromptInjection,
    bool EnableAudit,
    bool EnableMetrics,
    bool EnableCircuitBreaker)
{
    public static ResolvedGovernanceSettings Default { get; } =
        new(true, true, true, true, false);
}
