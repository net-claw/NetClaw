namespace NetClaw.Docker.Extensions;

public sealed record ExecResult
{
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public long ExitCode { get; init; }
    public bool Success { get; init; }
    public bool TimedOut { get; init; }
}
