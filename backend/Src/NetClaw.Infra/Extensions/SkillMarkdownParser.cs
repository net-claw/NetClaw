using System.Text.Json;
using Microsoft.Extensions.AI;

namespace NetClaw.Infra.Extensions;

public static class SkillMarkdownParser
{
    public static ParsedSkillDocument Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Skill content is required.");
        }

        var normalized = content.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("SKILL.md must start with a frontmatter block.");
        }

        var closingIndex = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (closingIndex < 0)
        {
            throw new InvalidOperationException("SKILL.md frontmatter is not closed.");
        }

        var frontmatterText = normalized[4..closingIndex];
        var body = normalized[(closingIndex + 5)..].Trim();
        var map = ParseFrontmatter(frontmatterText);

        var name = GetRequired(map, "name");
        var description = GetRequired(map, "description");
        var slug = map.GetValueOrDefault("slug");
        var version = map.GetValueOrDefault("version");
        var homepage = map.GetValueOrDefault("homepage");
        var license = map.GetValueOrDefault("license");
        var compatibility = map.GetValueOrDefault("compatibility");
        var allowedTools = map.GetValueOrDefault("allowedTools");
        var metadata = ParseMetadata(map.GetValueOrDefault("metadata"));

        return new ParsedSkillDocument(
            name,
            description,
            slug,
            version,
            homepage,
            license,
            compatibility,
            allowedTools,
            metadata,
            body);
    }

    public static string BuildSlug(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = new string(chars);

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }

    private static Dictionary<string, string> ParseFrontmatter(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            map[key] = value;
        }

        return map;
    }

    private static AdditionalPropertiesDictionary? ParseMetadata(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var metadata = new AdditionalPropertiesDictionary();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                metadata[property.Name] = property.Value.ToString();
            }

            return metadata;
        }
        catch
        {
            return null;
        }
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> map, string key)
    {
        if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"SKILL.md frontmatter is missing '{key}'.");
    }
}

public sealed record ParsedSkillDocument(
    string Name,
    string Description,
    string? Slug,
    string? Version,
    string? Homepage,
    string? License,
    string? Compatibility,
    string? AllowedTools,
    AdditionalPropertiesDictionary? Metadata,
    string Instructions);
