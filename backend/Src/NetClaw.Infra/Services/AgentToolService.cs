using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClaw.Application.Services;
using NetClaw.Docker.Extensions;
using NetClaw.Infra.Contexts;
using NetClaw.Infra.RuntimeSkills;

namespace NetClaw.Infra.Services;

public sealed class AgentToolService : IAgentToolService
{
    private readonly IReadOnlyList<AITool> _tools;
    private readonly ILogger<AgentToolService> _logger;
    private readonly IGovernanceService _governance;
    private readonly RuntimeSkillCatalog _runtimeSkillCatalog;
    private readonly ExcelSandboxService _excelSandboxService;
    private readonly DockerExecService _execService;
    private readonly IServiceScopeFactory _scopeFactory;

    public AgentToolService(
        ILogger<AgentToolService> logger,
        IGovernanceService governance,
        RuntimeSkillCatalog runtimeSkillCatalog,
        ExcelSandboxService excelSandboxService,
        DockerExecService execService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _governance = governance;
        _runtimeSkillCatalog = runtimeSkillCatalog;
        _excelSandboxService = excelSandboxService;
        _execService = execService;
        _scopeFactory = scopeFactory;
        _tools =
        [
            AIFunctionFactory.Create((string timezone) => GetServerTime(timezone), new AIFunctionFactoryOptions { Name = "get_server_time", Description = "Get the current server time in UTC or in a requested timezone like Asia/Ho_Chi_Minh or America/New_York." }),
            AIFunctionFactory.Create((double left, double right) => AddNumbers(left, right), new AIFunctionFactoryOptions { Name = "add_numbers", Description = "Add two numbers together and return the exact sum." }),
            AIFunctionFactory.Create(() => GetProjectSnapshot(), new AIFunctionFactoryOptions { Name = "get_project_snapshot", Description = "Return a short snapshot of the current NetClaw test environment and enabled capabilities." }),
            AIFunctionFactory.Create(() => GetAvailableRuntimeSkills(), new AIFunctionFactoryOptions { Name = "get_available_runtime_skills", Description = "List executable runtime skills discovered from uploaded skills metadata in the database." }),
            AIFunctionFactory.Create(() => GetAvailableAgentSkills(), new AIFunctionFactoryOptions { Name = "get_available_agent_skills", Description = "List uploaded agent skills from the database, including skill name, slug, description, and status." }),
            AIFunctionFactory.Create((string input) => EchoText(input), new AIFunctionFactoryOptions { Name = "echo_text", Description = "Echo the exact input string back. This is a governance test tool for validating prompt injection detection on raw tool arguments." }),
            AIFunctionFactory.Create((string title, string[] headers, string[][] rows, string? fileName, string? sheetName) => CreateExcelFileAsync(title, headers, rows, fileName, sheetName), new AIFunctionFactoryOptions { Name = "create_excel_file", Description = "Create a downloadable .xlsx Excel workbook from tabular data. Use this when the user asks for an Excel, XLSX, spreadsheet, workbook, export, or downloadable table file." }),
            AIFunctionFactory.Create((string command, int timeoutMs) => SandboxExecAsync(command, timeoutMs), new AIFunctionFactoryOptions { Name = "sandbox_exec", Description = "Run a shell command inside the sandbox container for CLI tasks that require shell execution. Prefer loading a relevant agent skill first when one is available, then follow that skill's instructions. Files written to /workspace/downloads/ are served at /api/v1/sandbox/downloads/<filename>." }),
        ];
    }

    public IReadOnlyList<AITool> GetTools() => _tools;

    private object GetServerTime(string timezone)
    {
        var blocked = EvaluateOrBlocked("get_server_time", new Dictionary<string, object> { ["timezone"] = timezone });
        if (blocked is not null) return blocked;

        try
        {
            var normalized = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim();
            var zone = normalized.Equals("UTC", StringComparison.OrdinalIgnoreCase) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(normalized);
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
            var result = new { timezone = zone.Id, current_time = now.ToString("O") };
            _logger.LogInformation("Tool invoked: get_server_time timezone={Timezone} result={Result}", timezone, result.current_time);
            return result;
        }
        catch
        {
            var result = new { timezone, error = "Unknown timezone.", utc_now = DateTimeOffset.UtcNow.ToString("O") };
            _logger.LogWarning("Tool invoked: get_server_time timezone={Timezone} error={Error}", timezone, result.error);
            return result;
        }
    }

    private object AddNumbers(double left, double right)
    {
        var blocked = EvaluateOrBlocked("add_numbers", new Dictionary<string, object> { ["left"] = left, ["right"] = right });
        if (blocked is not null) return blocked;

        var result = new { left, right, sum = left + right };
        _logger.LogInformation("Tool invoked: add_numbers left={Left} right={Right} sum={Sum}", left, right, result.sum);
        return result;
    }

    private object GetProjectSnapshot()
    {
        var blocked = EvaluateOrBlocked("get_project_snapshot");
        if (blocked is not null) return blocked;

        var dbSkills = GetDbSkills();
        var result = new
        {
            app = "NetClaw",
            test_surface = "TanStack SSE + team orchestration",
            available_tools = new[] { "get_server_time", "add_numbers", "get_project_snapshot", "get_available_runtime_skills", "get_available_agent_skills", "echo_text", "create_excel_file", "sandbox_exec" },
            available_modes = new[] { "general", "ops", "planner" },
            available_agent_skills = dbSkills.Select(skill => skill.Slug).ToArray(),
            available_runtime_skills = _runtimeSkillCatalog.GetSkills().Select(skill => skill.Key).ToArray(),
        };

        _logger.LogInformation("Tool invoked: get_project_snapshot");
        return result;
    }

    private object GetAvailableRuntimeSkills()
    {
        var blocked = EvaluateOrBlocked("get_available_runtime_skills");
        if (blocked is not null) return blocked;

        var skills = _runtimeSkillCatalog.GetSkills()
            .Select(skill => new
            {
                key = skill.Key,
                name = skill.Name,
                label = skill.Label,
                description = skill.Description,
                version = skill.Version,
                host_path = skill.HostPath,
                sandbox_path = skill.SandboxPath,
                actions = skill.Actions,
            })
            .ToArray();

        _logger.LogInformation("Tool invoked: get_available_runtime_skills count={Count}", skills.Length);
        return new { runtime_skills = skills };
    }

    private object GetAvailableAgentSkills()
    {
        var blocked = EvaluateOrBlocked("get_available_agent_skills");
        if (blocked is not null) return blocked;

        var skills = GetDbSkills();
        _logger.LogInformation("Tool invoked: get_available_agent_skills count={Count}", skills.Length);
        return new { agent_skills = skills };
    }

    private object EchoText(string input)
    {
        var blocked = EvaluateOrBlocked("echo_text", new Dictionary<string, object> { ["input"] = input });
        if (blocked is not null) return blocked;

        _logger.LogInformation("Tool invoked: echo_text");
        return new { echoed = input, length = input.Length };
    }

    private DbSkillSummary[] GetDbSkills()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return dbContext.Skills
            .AsNoTracking()
            .OrderBy(skill => skill.Name)
            .Select(skill => new DbSkillSummary(skill.Id, skill.Name, skill.Slug, skill.Description, skill.Status, skill.FileName, skill.ArchiveFileName))
            .ToArray();
    }

    private async Task<object> CreateExcelFileAsync(string title, string[] headers, string[][] rows, string? fileName, string? sheetName)
    {
        var blocked = EvaluateOrBlocked("create_excel_file", new Dictionary<string, object>
        {
            ["title"] = title,
            ["file_name"] = fileName ?? string.Empty,
            ["sheet_name"] = sheetName ?? string.Empty,
            ["header_count"] = headers.Length,
            ["row_count"] = rows.Length,
        });
        if (blocked is not null) return blocked;

        var resolvedTitle = string.IsNullOrWhiteSpace(title) ? "NetClaw Export" : title.Trim();
        var resolvedHeaders = headers is { Length: > 0 } ? headers : ["Name", "Email", "Score"];
        var resolvedRows = rows is { Length: > 0 }
            ? rows.Select(row => (IReadOnlyList<object?>)row.Cast<object?>().ToArray()).ToArray()
            : new IReadOnlyList<object?>[]
            {
                new object?[] { "Alice", "alice@example.com", 91 },
                new object?[] { "Bob", "bob@example.com", 88 },
                new object?[] { "Carol", "carol@example.com", 95 },
            };
        var resolvedFileName = string.IsNullOrWhiteSpace(fileName) ? "netclaw-export.xlsx" : fileName.Trim();
        var resolvedSheetName = string.IsNullOrWhiteSpace(sheetName) ? "Data" : sheetName.Trim();

        var install = await _excelSandboxService.InstallAsync();
        if (!install.Success)
        {
            _logger.LogWarning("Tool create_excel_file failed during dependency install: {Error}", install.Stderr);
            return new { success = false, error = "Failed to install Excel skill dependencies in sandbox.", stderr = install.Stderr, stdout = install.Stdout, timed_out = install.TimedOut };
        }

        var artifact = await _excelSandboxService.CreateWorkbookAsync(resolvedTitle, resolvedSheetName, resolvedFileName, resolvedHeaders, resolvedRows);

        _logger.LogInformation("Tool invoked: create_excel_file success={Success} file={FileName}", artifact.Success, artifact.FileName);

        return new
        {
            success = artifact.Success,
            file_name = artifact.FileName,
            download_path = artifact.DownloadPath,
            message = artifact.Success ? $"Excel workbook created successfully. Download it from {artifact.DownloadPath}." : "Failed to create Excel workbook.",
            stderr = artifact.Stderr,
            stdout = artifact.Stdout,
            timed_out = artifact.TimedOut,
        };
    }

    private async Task<object> SandboxExecAsync(string command, int timeoutMs = 30_000)
    {
        var blocked = EvaluateOrBlocked("sandbox_exec", new Dictionary<string, object> { ["command"] = command });
        if (blocked is not null) return blocked;

        _logger.LogInformation("Tool invoked: sandbox_exec command={Command} timeoutMs={TimeoutMs}", command, timeoutMs);

        var result = await _execService.RunAsync(
            ["sh", "-c", command],
            timeoutMs: Math.Clamp(timeoutMs, 1_000, 120_000));

        _logger.LogInformation("Tool sandbox_exec finished. success={Success} exitCode={ExitCode} timedOut={TimedOut}", result.Success, result.ExitCode, result.TimedOut);

        return new
        {
            success = result.Success,
            exit_code = result.ExitCode,
            stdout = result.Stdout.Trim(),
            stderr = result.Stderr.Trim(),
            timed_out = result.TimedOut,
        };
    }

    private object? EvaluateOrBlocked(string toolName, Dictionary<string, object>? args = null)
    {
        var result = _governance.EvaluateToolCall(toolName, args);
        if (result.Allowed) return null;

        _logger.LogWarning("Tool {ToolName} blocked by governance: {Reason}", toolName, result.Reason);
        return new { success = false, blocked_by_governance = true, tool_name = toolName, reason = result.Reason, action = result.Action, matched_rule = result.MatchedRule };
    }
}

internal sealed record DbSkillSummary(Guid Id, string Name, string Slug, string Description, string Status, string FileName, string? ArchiveFileName);
