using NetClaw.Api.Docker;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Docker.Extensions;
using NetClaw.Infra.RuntimeSkills;
using System.ComponentModel.DataAnnotations;

namespace NetClaw.Api.Endpoints;

public sealed class SandboxEndpoints : IEndpoint
{
    private static readonly IReadOnlyDictionary<string, string[]> ProbeCommands =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["python3"] = ["python3", "--version"],
            ["pip3"] = ["pip3", "--version"],
            ["node"] = ["node", "--version"],
            ["npm"] = ["npm", "--version"],
        };

    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/sandbox", async (SandboxManager sandboxManager, CancellationToken ct) =>
        {
            var containerId = await sandboxManager.EnsureAsync(ct);
            return Results.Ok(new
            {
                ok = true,
                container_id = containerId,
            });
        });

        group.MapGet("/sandbox/probe", async (DockerExecService execService, CancellationToken ct) =>
        {
            var results = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var allSucceeded = true;

            foreach (var pair in ProbeCommands)
            {
                var result = await execService.RunAsync(pair.Value, cancellationToken: ct);
                allSucceeded &= result.Success && !result.TimedOut;
                results[pair.Key] = new
                {
                    success = result.Success,
                    exit_code = result.ExitCode,
                    stdout = result.Stdout.Trim(),
                    stderr = result.Stderr.Trim(),
                    timed_out = result.TimedOut,
                };
            }

            return Results.Ok(new
            {
                ok = allSucceeded,
                tools = results,
            });
        });

        group.MapPost("/sandbox/probe/{tool}", async (string tool, DockerExecService execService, CancellationToken ct) =>
        {
            if (!ProbeCommands.TryGetValue(tool, out var command))
            {
                return Results.BadRequest(new
                {
                    error = $"Unsupported probe tool '{tool}'.",
                    supported_tools = ProbeCommands.Keys,
                });
            }

            var result = await execService.RunAsync(command, cancellationToken: ct);
            return Results.Ok(new
            {
                tool,
                result.Success,
                result.ExitCode,
                stdout = result.Stdout.Trim(),
                stderr = result.Stderr.Trim(),
                result.TimedOut,
            });
        });

        group.MapPost("/sandbox/python", async (
            SandboxPythonRequest request,
            DockerExecService execService,
            CancellationToken ct) =>
        {
            var validationError = Validate(request);
            if (validationError is not null)
            {
                return validationError;
            }

            var timeoutMs = NormalizeTimeout(request.TimeoutMs, 1_000, 60_000);
            var result = await execService.RunAsync(
                ["python3", "-c", request.Code],
                timeoutMs: timeoutMs,
                cancellationToken: ct);

            return Results.Ok(new
            {
                success = result.Success,
                exit_code = result.ExitCode,
                stdout = result.Stdout.Trim(),
                stderr = result.Stderr.Trim(),
                timed_out = result.TimedOut,
            });
        });

        group.MapPost("/sandbox/pip-install", async (
            SandboxPipInstallRequest request,
            DockerExecService execService,
            CancellationToken ct) =>
        {
            var validationError = Validate(request);
            if (validationError is not null)
            {
                return validationError;
            }

            var timeoutMs = NormalizeTimeout(request.TimeoutMs, 5_000, 300_000);
            var result = await execService.RunAsync(
                ["pip3", "install", request.Package, "--break-system-packages"],
                timeoutMs: timeoutMs,
                cancellationToken: ct);

            return Results.Ok(new
            {
                success = result.Success,
                exit_code = result.ExitCode,
                stdout = result.Stdout.Trim(),
                stderr = result.Stderr.Trim(),
                timed_out = result.TimedOut,
            });
        });

        group.MapPost("/sandbox/examples/excel-xlsx/install", async (
            ExcelSandboxService excelSandboxService,
            CancellationToken ct) =>
        {
            var result = await excelSandboxService.InstallAsync(ct);

            return Results.Ok(new
            {
                success = result.Success,
                exit_code = result.ExitCode,
                stdout = result.Stdout.Trim(),
                stderr = result.Stderr.Trim(),
                timed_out = result.TimedOut,
            });
        });

        group.MapPost("/sandbox/examples/excel-xlsx", async (
            ExcelSandboxCreateRequest request,
            ExcelSandboxService excelSandboxService,
            SandboxFileService fileService,
            CancellationToken ct) =>
        {
            var validationError = Validate(request);
            if (validationError is not null)
            {
                return validationError;
            }

            var result = await excelSandboxService.CreateWorkbookAsync(
                request.Title,
                request.SheetName,
                request.FileName,
                request.Headers,
                request.Rows,
                ct);

            if (!result.Success || result.TimedOut || string.IsNullOrWhiteSpace(result.FileName))
            {
                return Results.BadRequest(new
                {
                    success = result.Success,
                    exit_code = result.ExitCode,
                    stdout = result.Stdout.Trim(),
                    stderr = result.Stderr.Trim(),
                    timed_out = result.TimedOut,
                });
            }

            var outputPath = fileService.BuildSafeDownloadPath(result.FileName);
            return Results.File(
                fileContents: await File.ReadAllBytesAsync(outputPath, ct),
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: result.FileName);
        });

        group.MapGet("/sandbox/downloads/{fileName}", async (
            string fileName,
            SandboxFileService fileService,
            CancellationToken ct) =>
        {
            var path = fileService.BuildSafeDownloadPath(fileName);
            if (!File.Exists(path))
            {
                return Results.NotFound(new { error = $"File '{fileName}' was not found." });
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var (contentType, inline) = ext switch
            {
                ".png" => ("image/png", true),
                ".jpg" or ".jpeg" => ("image/jpeg", true),
                ".gif" => ("image/gif", true),
                ".webp" => ("image/webp", true),
                ".xlsx" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false),
                _ => ("application/octet-stream", false),
            };

            return Results.File(
                fileContents: await File.ReadAllBytesAsync(path, ct),
                contentType: contentType,
                fileDownloadName: inline ? null : Path.GetFileName(path));
        });
    }

    private static IResult? Validate<T>(T request)
    {
        var validationContext = new ValidationContext(request!);
        var errors = new List<ValidationResult>();
        if (Validator.TryValidateObject(request!, validationContext, errors, validateAllProperties: true))
        {
            return null;
        }

        return Results.ValidationProblem(errors
            .GroupBy(error => error.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(
                group => string.IsNullOrWhiteSpace(group.Key) ? "request" : group.Key,
                group => group.Select(error => error.ErrorMessage ?? "Validation error.").ToArray()));
    }

    private static int NormalizeTimeout(int timeoutMs, int min, int max) => Math.Clamp(timeoutMs, min, max);
}
