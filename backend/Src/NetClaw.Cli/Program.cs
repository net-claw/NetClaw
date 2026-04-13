using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.Cli.Commands;
using NetClaw.Cli.Infrastructure;
using NetClaw.Docker.Extensions;
using Spectre.Console.Cli;

if (args.Length == 0)
{
    OverviewScreen.Render();
    return 0;
}

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging();
services.AddDockerServices(configuration);

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("netclaw");

    config.AddCommand<ImageCommand>("image")
        .WithDescription("Manage container images");

    config.AddCommand<StatusCommand>("status")
        .WithDescription("Check application and container status");
});

return app.Run(args);
