using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.Docker.Extensions;
using NetClaw.Infra.Contexts;

namespace NetClaw.Infra.RuntimeSkills;

public sealed class RuntimeSkillCatalog
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SandboxPathResolver _pathResolver;

    public RuntimeSkillCatalog(IServiceScopeFactory scopeFactory, SandboxPathResolver pathResolver)
    {
        _scopeFactory = scopeFactory;
        _pathResolver = pathResolver;
    }

    public IReadOnlyList<RuntimeSkillDefinition> GetSkills()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var skillsHostDir = _pathResolver.GetSkillsHostDir();

        return dbContext.Skills
            .AsNoTracking()
            .Where(skill => skill.Status == "active")
            .ToList()
            .Select(skill => BuildDefinition(skill, skillsHostDir))
            .Where(skill => skill is not null)
            .Cast<RuntimeSkillDefinition>()
            .ToArray();
    }

    public RuntimeSkillDefinition? Find(string key) =>
        GetSkills().FirstOrDefault(skill => skill.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public RuntimeSkillDefinition? FindByAction(string action) =>
        GetSkills().FirstOrDefault(skill => skill.Actions.Any(item => item.Equals(action, StringComparison.OrdinalIgnoreCase)));

    private static RuntimeSkillDefinition? BuildDefinition(Domains.Entities.Skill skill, string skillsHostDir)
    {
        var inferred = InferDefinition(skill, skillsHostDir);
        if (string.IsNullOrWhiteSpace(skill.MetadataJson)) return inferred;

        try
        {
            using var document = JsonDocument.Parse(skill.MetadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return inferred;

            var root = document.RootElement;
            var runtimeRoot = root.TryGetProperty("runtime", out var runtimeProperty) &&
                              runtimeProperty.ValueKind == JsonValueKind.Object
                ? runtimeProperty
                : root;

            var hostPath = GetString(runtimeRoot, "hostPath") ?? GetString(root, "runtime_host_path");
            var sandboxPath = GetString(runtimeRoot, "sandboxPath") ?? GetString(root, "runtime_sandbox_path");
            var entryScript = GetString(runtimeRoot, "entryScript") ?? GetString(root, "runtime_entry_script");
            var requirementsFile = GetString(runtimeRoot, "requirementsFile") ?? GetString(root, "runtime_requirements_file") ?? "requirements.txt";
            var version = GetString(runtimeRoot, "version") ?? GetString(root, "runtime_version") ?? "1.0.0";
            var key = GetString(runtimeRoot, "key") ?? GetString(root, "runtime_key") ?? skill.Slug;
            var name = GetString(runtimeRoot, "name") ?? GetString(root, "runtime_name") ?? skill.Slug;
            var label = GetString(runtimeRoot, "label") ?? GetString(root, "runtime_label") ?? skill.Name;
            var actions = GetStringArray(runtimeRoot, "actions");
            if (actions.Count == 0)
            {
                actions = GetStringArray(root, "runtime_actions");
            }

            if (string.IsNullOrWhiteSpace(hostPath) || string.IsNullOrWhiteSpace(sandboxPath) || string.IsNullOrWhiteSpace(entryScript))
            {
                return inferred;
            }

            var resolvedHostPath = Path.IsPathRooted(hostPath) ? hostPath : Path.Combine(skillsHostDir, hostPath);

            return new RuntimeSkillDefinition(key, label, skill.Description, version, actions.Count > 0 ? actions : ["create_workbook"], name, resolvedHostPath, sandboxPath, entryScript, requirementsFile);
        }
        catch
        {
            return inferred;
        }
    }

    private static RuntimeSkillDefinition? InferDefinition(Domains.Entities.Skill skill, string skillsHostDir)
    {
        var uploadedDir = Path.Combine(skillsHostDir, "uploaded", skill.Slug);
        if (!Directory.Exists(uploadedDir)) return null;

        var entryScript = FindEntryScript(uploadedDir);
        if (entryScript is null) return null;

        var requirementsFile = FindRequirementsFile(uploadedDir);
        return new RuntimeSkillDefinition(skill.Slug, skill.Name, skill.Description, "1.0.0", ["create_workbook"], skill.Slug, uploadedDir, $"/skills/uploaded/{skill.Slug}", entryScript, requirementsFile);
    }

    private static string? FindEntryScript(string targetDir)
    {
        var candidates = new[]
        {
            Path.Combine(targetDir, "scripts", "create_workbook.py"),
            Path.Combine(targetDir, "create_workbook.py"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetRelativePath(targetDir, candidate).Replace('\\', '/');
            }
        }

        var namedScript = Directory.GetFiles(targetDir, "create_workbook.py", SearchOption.AllDirectories).OrderBy(path => path.Length).FirstOrDefault();
        if (namedScript is not null)
        {
            return Path.GetRelativePath(targetDir, namedScript).Replace('\\', '/');
        }

        return Directory.GetFiles(targetDir, "*.py", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(targetDir, path).Replace('\\', '/'))
            .OrderBy(path => path.Length)
            .FirstOrDefault();
    }

    private static string FindRequirementsFile(string targetDir)
    {
        var directPath = Path.Combine(targetDir, "requirements.txt");
        if (File.Exists(directPath)) return "requirements.txt";

        var nestedPath = Directory.GetFiles(targetDir, "requirements.txt", SearchOption.AllDirectories).OrderBy(path => path.Length).FirstOrDefault();
        return nestedPath is null ? string.Empty : Path.GetRelativePath(targetDir, nestedPath).Replace('\\', '/');
    }

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static IReadOnlyList<string> GetStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToArray();
    }
}

public sealed record RuntimeSkillDefinition(
    string Key,
    string Label,
    string Description,
    string Version,
    IReadOnlyList<string> Actions,
    string Name,
    string HostPath,
    string SandboxPath,
    string EntryScript,
    string RequirementsFile
);
