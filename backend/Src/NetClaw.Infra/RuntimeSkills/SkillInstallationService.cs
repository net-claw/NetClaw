using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NetClaw.Docker.Extensions;
using NetClaw.Domains.Entities;

namespace NetClaw.Infra.RuntimeSkills;

public sealed class SkillInstallationService(DockerExecService execService)
{
    private const string SandboxOs = "linux";
    private static readonly string SandboxArch = RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        Architecture.X86 => "x86",
        Architecture.Arm => "arm",
        _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
    };
    private static readonly Regex CommandNamePattern = new("^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

    public async Task<string?> RefreshInstallStateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        var manifest = SkillRuntimeManifest.Parse(skill.MetadataJson);
        if (manifest is null)
        {
            return skill.MetadataJson;
        }

        var installState = await BuildInstallStateAsync(manifest, cancellationToken);
        return UpsertInstallState(skill.MetadataJson, installState);
    }

    public async Task<SkillInstallExecutionResult> InstallAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        var manifest = SkillRuntimeManifest.Parse(skill.MetadataJson);
        if (manifest is null)
        {
            return new SkillInstallExecutionResult(
                false,
                UpsertInstallState(skill.MetadataJson, InstallState.NotApplicable()),
                "Skill does not define a supported runtime install manifest.",
                [],
                []);
        }

        var installSteps = manifest.InstallSteps
            .Where(step => IsSupportedOnSandbox(step.Os) && IsSupportedOnSandboxArch(step.Arch))
            .ToList();

        var stepResults = new List<SkillInstallStepResult>();
        foreach (var step in installSteps)
        {
            var result = await execService.RunAsync(step.Run, timeoutMs: 300_000, cancellationToken: cancellationToken);
            stepResults.Add(new SkillInstallStepResult(
                step.Id,
                step.Label,
                result.Success,
                result.ExitCode,
                result.Stdout.Trim(),
                result.Stderr.Trim(),
                result.TimedOut));

            if ((!result.Success || result.TimedOut) && !step.Optional)
            {
                if (IsPlatformIncompatibleError(result.Stderr))
                {
                    continue;
                }

                var failedState = await BuildInstallStateAsync(
                    manifest,
                    cancellationToken,
                    "failed",
                    $"Install step '{step.Id}' failed.",
                    stepResults,
                    []);

                return new SkillInstallExecutionResult(
                    false,
                    UpsertInstallState(skill.MetadataJson, failedState),
                    $"Install step '{step.Id}' failed.",
                    stepResults,
                    []);
            }
        }

        var verifyResults = new List<SkillInstallStepResult>();
        foreach (var verifyCommand in manifest.VerifyCommands)
        {
            var result = await execService.RunAsync(verifyCommand, timeoutMs: 60_000, cancellationToken: cancellationToken);
            verifyResults.Add(new SkillInstallStepResult(
                string.Join(" ", verifyCommand),
                "verify",
                result.Success,
                result.ExitCode,
                result.Stdout.Trim(),
                result.Stderr.Trim(),
                result.TimedOut));

            if (!result.Success || result.TimedOut)
            {
                var failedState = await BuildInstallStateAsync(
                    manifest,
                    cancellationToken,
                    "failed",
                    $"Verification failed for '{string.Join(" ", verifyCommand)}'.",
                    stepResults,
                    verifyResults);

                return new SkillInstallExecutionResult(
                    false,
                    UpsertInstallState(skill.MetadataJson, failedState),
                    $"Verification failed for '{string.Join(" ", verifyCommand)}'.",
                    stepResults,
                    verifyResults);
            }
        }

        var completedState = await BuildInstallStateAsync(
            manifest,
            cancellationToken,
            "installed",
            null,
            stepResults,
            verifyResults);

        return new SkillInstallExecutionResult(
            true,
            UpsertInstallState(skill.MetadataJson, completedState),
            null,
            stepResults,
            verifyResults);
    }

    private async Task<InstallState> BuildInstallStateAsync(
        SkillRuntimeManifest manifest,
        CancellationToken cancellationToken,
        string? forcedStatus = null,
        string? lastError = null,
        IReadOnlyList<SkillInstallStepResult>? installResults = null,
        IReadOnlyList<SkillInstallStepResult>? verifyResults = null)
    {
        var requiredCommands = manifest.RequiredCommands;
        var missingCommands = new List<string>();

        foreach (var command in requiredCommands)
        {
            if (!await CommandExistsAsync(command, cancellationToken))
            {
                missingCommands.Add(command);
            }
        }

        var now = DateTimeOffset.UtcNow.ToString("O");
        var status = forcedStatus
            ?? (requiredCommands.Count == 0
                ? "not_applicable"
                : missingCommands.Count == 0
                    ? "installed"
                    : "missing");

        return new InstallState(
            status,
            now,
            status == "installed" ? now : null,
            requiredCommands,
            missingCommands,
            lastError,
            installResults ?? [],
            verifyResults ?? []);
    }

    private async Task<bool> CommandExistsAsync(string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command) || !CommandNamePattern.IsMatch(command))
        {
            return false;
        }

        var result = await execService.RunAsync(
            ["sh", "-lc", $"command -v {command}"],
            timeoutMs: 10_000,
            cancellationToken: cancellationToken);

        return result.Success && !result.TimedOut;
    }

    private static bool IsSupportedOnSandbox(IReadOnlyList<string> osList) =>
        osList.Count == 0 || osList.Any(os => os.Equals(SandboxOs, StringComparison.OrdinalIgnoreCase));

    private static bool IsSupportedOnSandboxArch(IReadOnlyList<string> archList) =>
        archList.Count == 0 || archList.Any(arch => arch.Equals(SandboxArch, StringComparison.OrdinalIgnoreCase));

    // Patterns indicating a step was skipped due to platform incompatibility rather than a real failure.
    // Add Windows-specific patterns here when Windows sandbox support is added.
    private static readonly string[] PlatformIncompatiblePatterns =
    [
        // Linux ARM64 - Chrome for Testing
        "does not provide linux arm64 builds",
        // Generic cross-platform patterns
        "architecture not supported",
        "platform not supported",
        "not supported on this platform",
        "unsupported platform",
        "unsupported architecture",
    ];

    private static bool IsPlatformIncompatibleError(string stderr) =>
        PlatformIncompatiblePatterns.Any(p => stderr.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static string? UpsertInstallState(string? metadataJson, InstallState installState)
    {
        var root = !string.IsNullOrWhiteSpace(metadataJson)
            ? JsonNode.Parse(metadataJson)?.AsObject()
            : new JsonObject();
        root ??= new JsonObject();

        root["install"] = new JsonObject
        {
            ["status"] = installState.Status,
            ["lastCheckedAt"] = installState.LastCheckedAt,
            ["installedAt"] = installState.InstalledAt,
            ["lastError"] = installState.LastError,
            ["requiredCommands"] = ToStringArray(installState.RequiredCommands),
            ["missingCommands"] = ToStringArray(installState.MissingCommands),
            ["installResults"] = ToJsonArray(installState.InstallResults),
            ["verifyResults"] = ToJsonArray(installState.VerifyResults),
        };

        return root.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static JsonArray ToJsonArray(IReadOnlyList<SkillInstallStepResult> results)
    {
        var array = new JsonArray();
        foreach (var result in results)
        {
            array.Add(new JsonObject
            {
                ["id"] = result.Id,
                ["label"] = result.Label,
                ["success"] = result.Success,
                ["exitCode"] = result.ExitCode,
                ["stdout"] = result.Stdout,
                ["stderr"] = result.Stderr,
                ["timedOut"] = result.TimedOut,
            });
        }

        return array;
    }

    private static JsonArray ToStringArray(IReadOnlyList<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(JsonValue.Create(value));
        }

        return array;
    }
}

internal sealed record SkillRuntimeManifest(
    IReadOnlyList<string> RequiredCommands,
    IReadOnlyList<SkillInstallManifestStep> InstallSteps,
    IReadOnlyList<IReadOnlyList<string>> VerifyCommands)
{
    public static SkillRuntimeManifest? Parse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty("runtime", out var runtime) || runtime.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var requiredCommands = GetStringArray(runtime, "requiredCommands");
            var installSteps = GetInstallSteps(runtime);
            var verifyCommands = GetNestedCommandArray(runtime, "verify", "commands");

            return new SkillRuntimeManifest(requiredCommands, installSteps, verifyCommands);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<SkillInstallManifestStep> GetInstallSteps(JsonElement runtime)
    {
        if (!runtime.TryGetProperty("install", out var install) || install.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!install.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return steps.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(item =>
            {
                var run = GetStringArray(item, "run");
                return new SkillInstallManifestStep(
                    GetString(item, "id") ?? Guid.NewGuid().ToString("n"),
                    GetString(item, "label") ?? "install",
                    GetStringArray(item, "os"),
                    GetStringArray(item, "arch"),
                    GetBool(item, "optional"),
                    run);
            })
            .Where(step => step.Run.Count > 0)
            .ToArray();
    }

    private static IReadOnlyList<IReadOnlyList<string>> GetNestedCommandArray(JsonElement runtime, string propertyName, string nestedPropertyName)
    {
        if (!runtime.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!property.TryGetProperty(nestedPropertyName, out var commands) || commands.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return commands.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Array)
            .Select(item => item.EnumerateArray()
                .Where(part => part.ValueKind == JsonValueKind.String)
                .Select(part => part.GetString())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Cast<string>()
                .ToArray() as IReadOnlyList<string>)
            .Where(command => command.Count > 0)
            .ToArray();
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToArray();
    }

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool GetBool(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True;
}

internal sealed record SkillInstallManifestStep(
    string Id,
    string Label,
    IReadOnlyList<string> Os,
    IReadOnlyList<string> Arch,
    bool Optional,
    IReadOnlyList<string> Run);

internal sealed record InstallState(
    string Status,
    string LastCheckedAt,
    string? InstalledAt,
    IReadOnlyList<string> RequiredCommands,
    IReadOnlyList<string> MissingCommands,
    string? LastError,
    IReadOnlyList<SkillInstallStepResult> InstallResults,
    IReadOnlyList<SkillInstallStepResult> VerifyResults)
{
    public static InstallState NotApplicable() =>
        new("not_applicable", DateTimeOffset.UtcNow.ToString("O"), null, [], [], null, [], []);
}

public sealed record SkillInstallExecutionResult(
    bool Success,
    string? MetadataJson,
    string? Error,
    IReadOnlyList<SkillInstallStepResult> InstallResults,
    IReadOnlyList<SkillInstallStepResult> VerifyResults);

public sealed record SkillInstallStepResult(
    string Id,
    string Label,
    bool Success,
    long ExitCode,
    string Stdout,
    string Stderr,
    bool TimedOut);
