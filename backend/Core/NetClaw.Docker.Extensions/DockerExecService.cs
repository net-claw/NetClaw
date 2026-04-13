using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace NetClaw.Docker.Extensions;

public sealed class DockerExecService(
    DockerClient docker,
    SandboxManager sandboxManager,
    ILogger<DockerExecService> logger)
{
    public async Task<ExecResult> RunAsync(
        IReadOnlyList<string> command,
        string workingDirectory = "/workspace",
        int timeoutMs = 15_000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Count == 0)
        {
            throw new ArgumentException("Command must not be empty.", nameof(command));
        }

        var containerId = await sandboxManager.EnsureAsync(cancellationToken);
        var exec = await docker.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
        {
            AttachStdout = true,
            AttachStderr = true,
            WorkingDir = workingDirectory,
            Cmd = command.ToList(),
        }, cancellationToken);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeoutMs);

        try
        {
            using var stream = await docker.Exec.StartAndAttachContainerExecAsync(exec.ID, false, linkedCts.Token);
            var (stdout, stderr) = await ReadToEndAsync(stream, linkedCts.Token);
            var inspect = await docker.Exec.InspectContainerExecAsync(exec.ID, linkedCts.Token);

            return new ExecResult
            {
                Stdout = stdout,
                Stderr = stderr,
                ExitCode = inspect.ExitCode,
                Success = inspect.ExitCode == 0,
            };
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Sandbox command timed out: {Command}", string.Join(" ", command));
            return new ExecResult
            {
                TimedOut = true,
                Success = false,
                Stderr = $"Timed out after {timeoutMs}ms.",
            };
        }
    }

    private static async Task<(string stdout, string stderr)> ReadToEndAsync(MultiplexedStream stream, CancellationToken cancellationToken)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var buffer = new byte[8 * 1024];

        while (true)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
            if (result.EOF)
            {
                break;
            }

            var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (result.Target == MultiplexedStream.TargetStream.StandardOut)
            {
                stdout.Append(chunk);
            }
            else if (result.Target == MultiplexedStream.TargetStream.StandardError)
            {
                stderr.Append(chunk);
            }
        }

        return (stdout.ToString(), stderr.ToString());
    }
}
