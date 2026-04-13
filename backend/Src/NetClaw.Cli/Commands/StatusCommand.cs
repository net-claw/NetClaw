using Docker.DotNet;
using Docker.DotNet.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetClaw.Cli.Commands;

internal sealed class StatusCommand(DockerClient docker) : Command
{
    protected override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        return ExecuteAsync(cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var version = await docker.System.GetVersionAsync(cancellationToken);
            AnsiConsole.MarkupLine("[green]Docker daemon:[/] connected");
            AnsiConsole.MarkupLine($"[grey]Server version:[/] {Markup.Escape(version.Version ?? "unknown")}");
            AnsiConsole.MarkupLine($"[grey]API version:[/] {Markup.Escape(version.APIVersion ?? "unknown")}");
            AnsiConsole.WriteLine();

            var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
            }, cancellationToken);

            var appContainers = containers
                .Where(IsNetClawContainer)
                .OrderBy(container => GetPrimaryName(container))
                .ToList();

            if (appContainers.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No NetClaw containers found.[/]");
                return 0;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Container")
                .AddColumn("Image")
                .AddColumn("State")
                .AddColumn("Status");

            foreach (var container in appContainers)
            {
                table.AddRow(
                    Markup.Escape(GetPrimaryName(container)),
                    Markup.Escape(container.Image),
                    FormatState(container.State),
                    Markup.Escape(container.Status));
            }

            AnsiConsole.MarkupLine("[bold deepskyblue2]NetClaw Containers[/]");
            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Docker daemon:[/] unavailable");
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(ex.Message)}[/]");
            return -1;
        }
    }

    private static bool IsNetClawContainer(ContainerListResponse container)
    {
        var matchesName = container.Names.Any(name =>
            name.Contains("netclaw", StringComparison.OrdinalIgnoreCase));

        var matchesImage = container.Image.Contains("netclaw", StringComparison.OrdinalIgnoreCase);
        return matchesName || matchesImage;
    }

    private static string GetPrimaryName(ContainerListResponse container)
    {
        return container.Names.FirstOrDefault()?.TrimStart('/') ?? container.ID[..12];
    }

    private static string FormatState(string? state)
    {
        return state?.ToLowerInvariant() switch
        {
            "running" => "[green]running[/]",
            "exited" => "[red]exited[/]",
            "created" => "[yellow]created[/]",
            "paused" => "[yellow]paused[/]",
            "restarting" => "[orange1]restarting[/]",
            _ => Markup.Escape(state ?? "unknown"),
        };
    }
}
