namespace NetClaw.Application.Services;

public interface IGovernanceService
{
    string PolicyPath { get; }

    GovernanceToolEvaluation EvaluateToolCall(string toolName, Dictionary<string, object>? args = null);

    GovernancePromptInjectionDetection? DetectPromptInjection(string? input);

    void InvalidateSettingsCache();
}

public sealed record GovernanceToolEvaluation(
    bool Allowed,
    string Reason,
    string? Action,
    string? MatchedRule);

public sealed record GovernancePromptInjectionDetection(
    bool IsInjection,
    string? InjectionType,
    string? ThreatLevel,
    double Confidence,
    string? Explanation);
