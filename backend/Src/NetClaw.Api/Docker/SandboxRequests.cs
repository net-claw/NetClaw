using System.ComponentModel.DataAnnotations;

namespace NetClaw.Api.Docker;

public sealed record SandboxPythonRequest
{
    [Required]
    public string Code { get; init; } = string.Empty;

    public int TimeoutMs { get; init; } = 15_000;
}

public sealed record SandboxPipInstallRequest
{
    [Required]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._\-<>=!~]*$", ErrorMessage = "Package contains unsupported characters.")]
    public string Package { get; init; } = string.Empty;

    public int TimeoutMs { get; init; } = 120_000;
}
