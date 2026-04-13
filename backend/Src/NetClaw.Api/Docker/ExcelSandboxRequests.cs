using System.ComponentModel.DataAnnotations;

namespace NetClaw.Api.Docker;

public sealed record ExcelSandboxCreateRequest
{
    [Required]
    public string Title { get; init; } = "NetClaw Export";

    public string SheetName { get; init; } = "Data";

    public string FileName { get; init; } = "netclaw-export.xlsx";

    public string[] Headers { get; init; } = ["Name", "Email", "Score"];

    public object[][] Rows { get; init; } =
    [
        ["Alice", "alice@example.com", 91],
        ["Bob", "bob@example.com", 88],
        ["Carol", "carol@example.com", 95],
    ];

    public int TimeoutMs { get; init; } = 60_000;
}
