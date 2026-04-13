using Spectre.Console;

namespace NetClaw.Cli.Commands;

internal static class OverviewScreen
{
    public static void Render()
    {
        RenderLogo();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[black on deepskyblue2 bold] NETCLAW CLI [/]");
        AnsiConsole.MarkupLine("[grey]Terminal control surface for NetClaw containers and API runtime.[/]");
        AnsiConsole.WriteLine();

        RenderCommands();
        AnsiConsole.WriteLine();
        RenderExamples();
    }

    private static void RenderLogo()
    {
        var figlet = new FigletText("NetClaw")
            .LeftJustified()
            .Color(Color.DeepSkyBlue2);

        AnsiConsole.Write(figlet);
    }

    private static void RenderCommands()
    {
        AnsiConsole.MarkupLine("[bold deepskyblue2]Commands[/]");

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn(string.Empty).NoWrap())
            .AddColumn(string.Empty);

        table.AddRow("[white on deepskyblue4 bold] image  [/]", "[grey]Manage container images[/]");
        table.AddRow("[white on deepskyblue4 bold] status [/]", "[grey]Check app and container status[/]");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Run `netclaw <command> --help` for command details.[/]");
    }

    private static void RenderExamples()
    {
        AnsiConsole.MarkupLine("[bold deepskyblue2]Examples[/]");
        AnsiConsole.MarkupLine("[grey]  netclaw image[/]");
        AnsiConsole.MarkupLine("[grey]  netclaw status[/]");
    }
}
