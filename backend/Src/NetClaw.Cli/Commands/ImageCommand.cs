using Spectre.Console;
using Spectre.Console.Cli;

namespace NetClaw.Cli.Commands;

internal sealed class ImageCommand : Command
{
    protected override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]`image` chưa được implement.[/]");
        AnsiConsole.MarkupLine("[grey]Đây là placeholder để chốt flow CLI trước khi nối logic quản lý container image.[/]");
        return 0;
    }
}
