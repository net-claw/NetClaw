using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NetClaw.Docker.Extensions;

public sealed class SandboxManager(
    DockerClient docker,
    IConfiguration configuration,
    SandboxPathResolver pathResolver,
    ILogger<SandboxManager> logger)
{
    private string? _containerId;

    public string ContainerId => _containerId ?? throw new InvalidOperationException("Sandbox is not ready.");

    public async Task<string> EnsureAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_containerId))
        {
            return _containerId;
        }

        var containerName = configuration["SANDBOX_NAME"] ?? "netclaw-sandbox";
        var existing = await FindByNameAsync(containerName, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.State, "running", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Starting existing sandbox container {ContainerName}", containerName);
                await docker.Containers.StartContainerAsync(existing.ID, new ContainerStartParameters(), cancellationToken);
            }

            _containerId = existing.ID;
            return _containerId;
        }

        var image = configuration["SANDBOX_IMAGE"] ?? "netclaw-sandbox:latest";
        logger.LogInformation("Creating sandbox container {ContainerName} from {Image}", containerName, image);
        var sharedDir = pathResolver.GetSharedDir();
        var skillsDir = pathResolver.GetSkillsHostDir();
        Directory.CreateDirectory(sharedDir);

        var create = await docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = containerName,
            Image = image,
            Cmd = ["sleep", "infinity"],
            WorkingDir = "/workspace",
            HostConfig = new HostConfig
            {
                Binds =
                [
                    $"{sharedDir}:/workspace",
                    $"{skillsDir}:/skills:ro",
                ],
                Memory = 512 * 1024 * 1024,
                NanoCPUs = 1_000_000_000,
            },
        }, cancellationToken);

        await docker.Containers.StartContainerAsync(create.ID, new ContainerStartParameters(), cancellationToken);
        _containerId = create.ID;
        return _containerId;
    }

    private async Task<ContainerListResponse?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
        }, cancellationToken);

        return containers.FirstOrDefault(container =>
            container.Names.Any(containerName => string.Equals(containerName, $"/{name}", StringComparison.Ordinal)));
    }
}
