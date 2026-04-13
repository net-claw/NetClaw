using Microsoft.Extensions.Configuration;

namespace NetClaw.Docker.Extensions;

public sealed class SandboxPathResolver(IConfiguration configuration)
{
    public string GetSharedDir()
    {
        var configured = configuration["SANDBOX_SHARED_DIR"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return Path.Combine(GetRepoRoot(), "sandbox-data");
    }

    public string GetSkillsHostDir()
    {
        var configured = configuration["SKILLS_DIR"];
        if (!string.IsNullOrWhiteSpace(configured) && Path.IsPathRooted(configured) && Directory.Exists(configured))
        {
            return configured;
        }

        return Path.Combine(GetRepoRoot(), "skills");
    }

    private string GetRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var dockerCompose = Path.Combine(current.FullName, "docker-compose.yml");
            var skillsDir = Path.Combine(current.FullName, "skills");
            if (File.Exists(dockerCompose) && Directory.Exists(skillsDir))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
