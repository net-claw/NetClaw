using System.Text.Json;

namespace NetClaw.Infra.Extensions;

public static class SkillPackageMetadataParser
{
    public static ParsedSkillPackageMetadata? Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = document.RootElement;
            return new ParsedSkillPackageMetadata(
                GetString(root, "ownerId"),
                GetString(root, "slug"),
                GetString(root, "version"),
                GetInt64(root, "publishedAt"),
                root.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static long? GetInt64(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var value)
            ? value
            : null;
}

public sealed record ParsedSkillPackageMetadata(
    string? OwnerId,
    string? Slug,
    string? Version,
    long? PublishedAt,
    string RawJson);
