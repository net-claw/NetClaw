using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetClaw.Docker.Extensions;

namespace NetClaw.Infra.RuntimeSkills;

public sealed class ExcelSandboxService
{
    private readonly DockerExecService _execService;
    private readonly SandboxFileService _fileService;
    private readonly RuntimeSkillCatalog _runtimeSkillCatalog;
    private readonly ILogger<ExcelSandboxService> _logger;

    public ExcelSandboxService(
        DockerExecService execService,
        SandboxFileService fileService,
        RuntimeSkillCatalog runtimeSkillCatalog,
        ILogger<ExcelSandboxService> logger)
    {
        _execService = execService;
        _fileService = fileService;
        _runtimeSkillCatalog = runtimeSkillCatalog;
        _logger = logger;
    }

    public async Task<ExcelSandboxArtifact> InstallAsync(CancellationToken ct = default)
    {
        var skill = ResolveExcelSkill();
        var requirementsPath = BuildSandboxSkillPath(skill, skill.RequirementsFile);
        var hostRequirementsPath = Path.Combine(skill.HostPath, skill.RequirementsFile);

        if (!File.Exists(hostRequirementsPath) || string.IsNullOrWhiteSpace(await File.ReadAllTextAsync(hostRequirementsPath, ct)))
        {
            _logger.LogInformation("Excel sandbox install skipped. skill={SkillKey} requirements file is missing or empty.", skill.Key);
            return new ExcelSandboxArtifact(true, null, null, 0, "Skipped dependency install.", string.Empty, false);
        }

        _logger.LogInformation("Excel sandbox install started. skill={SkillKey} requirements={RequirementsPath}", skill.Key, requirementsPath);

        var result = await _execService.RunAsync(["pip3", "install", "-r", requirementsPath, "--break-system-packages"], timeoutMs: 300_000, cancellationToken: ct);

        _logger.LogInformation(
            "Excel sandbox install finished. success={Success} exitCode={ExitCode} timedOut={TimedOut} stdout={Stdout} stderr={Stderr}",
            result.Success,
            result.ExitCode,
            result.TimedOut,
            TrimForLog(result.Stdout),
            TrimForLog(result.Stderr));

        return new ExcelSandboxArtifact(result.Success, null, null, result.ExitCode, result.Stdout.Trim(), result.Stderr.Trim(), result.TimedOut);
    }

    public async Task<ExcelSandboxArtifact> CreateWorkbookAsync(
        string title,
        string sheetName,
        string fileName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        CancellationToken ct = default)
    {
        var outputPath = _fileService.BuildSafeDownloadPath(fileName);
        var sandboxOutputPath = $"/workspace/downloads/{Path.GetFileName(outputPath)}";
        _logger.LogInformation(
            "Excel workbook creation started. title={Title} sheet={SheetName} fileName={FileName} hostOutput={HostOutput} sandboxOutput={SandboxOutput}",
            title,
            sheetName,
            fileName,
            outputPath,
            sandboxOutputPath);

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var skill = ResolveExcelSkill();
        var skillScriptPath = BuildSandboxSkillPath(skill, skill.EntryScript);
        var payload = JsonSerializer.Serialize(new { headers, rows });

        var result = await _execService.RunAsync(
            ["python3", skillScriptPath, "--output", sandboxOutputPath, "--title", title, "--sheet", sheetName, "--json", payload],
            timeoutMs: 120_000,
            cancellationToken: ct);

        var safeName = Path.GetFileName(outputPath);
        var downloadPath = $"/api/v1/sandbox/downloads/{safeName}";
        var fileExists = File.Exists(outputPath);

        _logger.LogInformation(
            "Excel workbook creation finished. success={Success} exitCode={ExitCode} timedOut={TimedOut} fileExists={FileExists} downloadPath={DownloadPath} stdout={Stdout} stderr={Stderr}",
            result.Success,
            result.ExitCode,
            result.TimedOut,
            fileExists,
            downloadPath,
            TrimForLog(result.Stdout),
            TrimForLog(result.Stderr));

        if (!result.Success || result.TimedOut || !fileExists)
        {
            return new ExcelSandboxArtifact(
                false,
                safeName,
                downloadPath,
                result.ExitCode,
                result.Stdout.Trim(),
                string.IsNullOrWhiteSpace(result.Stderr) ? $"Workbook was not created at '{outputPath}'." : result.Stderr.Trim(),
                result.TimedOut);
        }

        return new ExcelSandboxArtifact(true, safeName, downloadPath, result.ExitCode, result.Stdout.Trim(), result.Stderr.Trim(), result.TimedOut);
    }

    private RuntimeSkillDefinition ResolveExcelSkill() =>
        _runtimeSkillCatalog.FindByAction("create_workbook")
        ?? throw new InvalidOperationException("No runtime skill with action 'create_workbook' was found.");

    private static string BuildSandboxSkillPath(RuntimeSkillDefinition skill, string relativePath)
    {
        var sanitizedRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
        return $"{skill.SandboxPath.TrimEnd('/')}/{sanitizedRelativePath}";
    }

    private static string TrimForLog(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.Trim();
        return value.Length <= 400 ? value : value[..400];
    }
}

public sealed record ExcelSandboxArtifact(
    bool Success,
    string? FileName,
    string? DownloadPath,
    long ExitCode,
    string Stdout,
    string Stderr,
    bool TimedOut
);
