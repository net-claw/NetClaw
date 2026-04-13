using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NetClaw.Docker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDockerServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dockerHost = configuration["DOCKER_HOST"] ?? "unix:///var/run/docker.sock";

        services.AddSingleton(_ => new DockerClientConfiguration(new Uri(dockerHost)).CreateClient());
        services.AddSingleton<SandboxPathResolver>();
        services.AddSingleton<SandboxManager>();
        services.AddSingleton<DockerExecService>();
        services.AddSingleton<SandboxFileService>();

        return services;
    }
}
