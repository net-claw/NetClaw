using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetClaw.Docker.Extensions;
using NetClaw.Api.Endpoints.Abstractions;
using NetClaw.Contracts.Llm.Skills;
using NetClaw.Domains.Entities;
using NetClaw.Domains.Repos;
using NetClaw.Infra.Extensions;
using NetClaw.Infra.RuntimeSkills;

namespace NetClaw.Api.Endpoints;

public sealed class LlmSkillEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/llm/skills", async (
            [AsParameters] GetSkillsRequest request,
            HttpContext context,
            ISkillRepo repo,
            CancellationToken ct) =>
        {
            var pageIndex = Math.Max(request.PageIndex ?? 0, 0);
            var pageSize = Math.Clamp(request.PageSize ?? 10, 1, 100);
            var ascending = request.Ascending ?? true;
            var searchText = request.SearchText?.Trim();
            var status = request.Status?.Trim().ToLowerInvariant();

            var query = repo.Query();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(skill =>
                    EF.Functions.ILike(skill.Name, $"%{searchText}%") ||
                    EF.Functions.ILike(skill.Slug, $"%{searchText}%") ||
                    EF.Functions.ILike(skill.FileName, $"%{searchText}%") ||
                    EF.Functions.ILike(skill.Description, $"%{searchText}%"));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(skill => skill.Status == status);
            }

            query = (request.OrderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "file_name" => ascending
                    ? query.OrderBy(skill => skill.FileName).ThenBy(skill => skill.Name)
                    : query.OrderByDescending(skill => skill.FileName).ThenByDescending(skill => skill.Name),
                "updated_at" => ascending
                    ? query.OrderBy(skill => skill.UpdatedOn ?? skill.CreatedOn).ThenBy(skill => skill.Name)
                    : query.OrderByDescending(skill => skill.UpdatedOn ?? skill.CreatedOn).ThenByDescending(skill => skill.Name),
                "status" => ascending
                    ? query.OrderBy(skill => skill.Status).ThenBy(skill => skill.Name)
                    : query.OrderByDescending(skill => skill.Status).ThenByDescending(skill => skill.Name),
                _ => ascending
                    ? query.OrderBy(skill => skill.Name)
                    : query.OrderByDescending(skill => skill.Name),
            };

            var totalItems = await query.CountAsync(ct);
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ApiResults.Ok(
                context,
                new PagedResponse<SkillResponse>(
                    items.Select(ToResponse).ToList(),
                    pageIndex,
                    pageSize,
                    totalItems,
                    totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)));
        }).RequireAuthorization();

        group.MapGet("/llm/skills/{skillId:guid}", async (
            Guid skillId,
            HttpContext context,
            ISkillRepo repo,
            CancellationToken ct) =>
        {
            var skill = await repo.FindAsync(skillId, ct);
            return skill is null
                ? ApiResults.Error(context, StatusCodes.Status404NotFound, "Skill not found.")
                : ApiResults.Ok(context, ToResponse(skill));
        }).RequireAuthorization();

        group.MapPost("/llm/skills", async (
            CreateSkillRequest request,
            HttpContext context,
            ISkillRepo repo,
            SkillInstallationService installationService,
            CancellationToken ct) =>
        {
            var validationError = await ValidateRequestAsync(request.Name, request.Slug, request.File_Name, request.Content, request.Status, request.Metadata_Json, repo, null, ct);
            if (validationError is not null)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, validationError);
            }

            var metadataJson = await installationService.RefreshInstallStateAsync(
                new Skill(
                    request.Name,
                    request.Slug,
                    request.Description,
                    request.File_Name,
                    request.Content,
                    request.Status,
                    request.Metadata_Json),
                ct);

            var skill = new Skill(
                request.Name,
                request.Slug,
                request.Description,
                request.File_Name,
                request.Content,
                request.Status,
                metadataJson);

            await repo.AddAsync(skill, ct);
            await repo.SaveChangesAsync(ct);

            return ApiResults.Ok(context, ToResponse(skill));
        }).RequireAuthorization();

        group.MapPut("/llm/skills/{skillId:guid}", async (
            Guid skillId,
            UpdateSkillRequest request,
            HttpContext context,
            ISkillRepo repo,
            SkillInstallationService installationService,
            CancellationToken ct) =>
        {
            var skill = await repo.FindAsync(skillId, ct);
            if (skill is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "Skill not found.");
            }

            var validationError = await ValidateRequestAsync(request.Name, request.Slug, request.File_Name, request.Content, request.Status, request.Metadata_Json, repo, skillId, ct);
            if (validationError is not null)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, validationError);
            }

            var metadataJson = await installationService.RefreshInstallStateAsync(
                new Skill(
                    request.Name,
                    request.Slug,
                    request.Description,
                    request.File_Name,
                    request.Content,
                    request.Status,
                    request.Metadata_Json),
                ct);

            skill.Update(
                request.Name,
                request.Slug,
                request.Description,
                request.File_Name,
                request.Content,
                request.Status,
                metadataJson,
                skill.ArchiveFileName);

            await repo.SaveChangesAsync(ct);

            return ApiResults.Ok(context, ToResponse(skill));
        }).RequireAuthorization();

        group.MapDelete("/llm/skills/{skillId:guid}", async (
            Guid skillId,
            ISkillRepo repo,
            SandboxPathResolver pathResolver,
            CancellationToken ct) =>
        {
            var skill = await repo.FindAsync(skillId, ct);
            if (skill is null)
            {
                return Results.NoContent();
            }

            DeleteExtractedSkillDirectory(pathResolver, skill.Slug);
            repo.Delete(skill);
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/llm/skills/upload", async (
            HttpRequest request,
            HttpContext context,
            ISkillRepo repo,
            SandboxPathResolver pathResolver,
            SkillInstallationService installationService,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Multipart form data is required.");
            }

            var form = await request.ReadFormAsync(ct);
            var file = form.Files["file"] ?? form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Upload file is required.");
            }

            string fileName;
            string content;
            string? archiveFileName = null;
            string? metadataJson = null;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension == ".zip")
            {
                archiveFileName = Path.GetFileName(file.FileName);
                (fileName, content, metadataJson) = await ExtractSkillArchiveAsync(file, pathResolver, ct);
            }
            else if (extension == ".md")
            {
                fileName = Path.GetFileName(file.FileName);
                using var reader = new StreamReader(file.OpenReadStream());
                content = await reader.ReadToEndAsync(ct);
            }
            else
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, "Only .zip and .md files are supported.");
            }

            ParsedSkillDocument parsed;
            try
            {
                parsed = SkillMarkdownParser.Parse(content);
            }
            catch (Exception ex)
            {
                return ApiResults.Error(context, StatusCodes.Status400BadRequest, ex.Message);
            }

            var slug = ResolveSkillSlug(parsed, metadataJson);
            metadataJson = MergeMetadataJson(metadataJson, parsed);
            metadataJson = await installationService.RefreshInstallStateAsync(
                new Skill(
                    parsed.Name,
                    slug,
                    parsed.Description,
                    fileName,
                    content,
                    "active",
                    metadataJson,
                    archiveFileName),
                ct);

            var existing = await repo.FindAsync(skill => skill.Slug == slug, ct);
            if (existing is not null)
            {
                existing.Update(
                    parsed.Name,
                    slug,
                    parsed.Description,
                    fileName,
                    content,
                    existing.Status,
                    metadataJson,
                    archiveFileName);

                await repo.SaveChangesAsync(ct);
                return ApiResults.Ok(context, ToResponse(existing));
            }

            var skill = new Skill(
                parsed.Name,
                slug,
                parsed.Description,
                fileName,
                content,
                "active",
                metadataJson,
                archiveFileName);

            await repo.AddAsync(skill, ct);
            await repo.SaveChangesAsync(ct);

            return ApiResults.Ok(context, ToResponse(skill));
        }).RequireAuthorization();

        group.MapPost("/llm/skills/{skillId:guid}/install", async (
            Guid skillId,
            HttpContext context,
            ISkillRepo repo,
            SkillInstallationService installationService,
            CancellationToken ct) =>
        {
            var skill = await repo.FindAsync(skillId, ct);
            if (skill is null)
            {
                return ApiResults.Error(context, StatusCodes.Status404NotFound, "Skill not found.");
            }

            var result = await installationService.InstallAsync(skill, ct);
            skill.Update(
                skill.Name,
                skill.Slug,
                skill.Description,
                skill.FileName,
                skill.Content,
                skill.Status,
                result.MetadataJson,
                skill.ArchiveFileName);

            await repo.SaveChangesAsync(ct);

            if (!result.Success)
            {
                return ApiResults.Error(
                    context,
                    StatusCodes.Status400BadRequest,
                    result.Error ?? "Skill installation failed.");
            }

            return ApiResults.Ok(context, ToResponse(skill));
        }).RequireAuthorization();
    }

    private static async Task<(string fileName, string content, string? metadataJson)> ExtractSkillArchiveAsync(
        IFormFile file,
        SandboxPathResolver pathResolver,
        CancellationToken ct)
    {
        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, ct);
        memory.Position = 0;

        using var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.Entries.FirstOrDefault(item =>
            item.Name.Equals("SKILL.md", StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new InvalidOperationException("Archive must contain a SKILL.md file.");
        }

        string content;
        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        content = await reader.ReadToEndAsync(ct);

        var parsed = SkillMarkdownParser.Parse(content);
        var packageMetadataEntry = archive.Entries.FirstOrDefault(item =>
            item.Name.Equals("_meta.json", StringComparison.OrdinalIgnoreCase));
        string? packageMetadataContent = null;
        ParsedSkillPackageMetadata? packageMetadata = null;
        if (packageMetadataEntry is not null)
        {
            await using var packageMetadataStream = packageMetadataEntry.Open();
            using var packageMetadataReader = new StreamReader(packageMetadataStream);
            packageMetadataContent = await packageMetadataReader.ReadToEndAsync(ct);
            packageMetadata = SkillPackageMetadataParser.Parse(packageMetadataContent);
        }

        var slug = ResolveSkillSlug(parsed, packageMetadata);
        var skillsHostDir = pathResolver.GetSkillsHostDir();
        var targetDir = Path.Combine(skillsHostDir, "uploaded", slug);

        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }

        Directory.CreateDirectory(targetDir);
        memory.Position = 0;
        var rootFolder = GetSingleTopLevelFolder(archive.Entries);

        using (var extractionArchive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: true))
        {
            foreach (var archiveEntry in extractionArchive.Entries)
            {
                var relativePath = archiveEntry.FullName.Replace('\\', '/').TrimStart('/');
                if (ShouldSkipArchiveEntry(relativePath))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rootFolder) &&
                    relativePath.StartsWith($"{rootFolder}/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = relativePath[(rootFolder.Length + 1)..];
                }

                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    continue;
                }

                var destinationPath = Path.GetFullPath(Path.Combine(targetDir, relativePath));
                if (!destinationPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.Ordinal))
                {
                    continue;
                }

                if (archiveEntry.FullName.EndsWith("/", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                await using var source = archiveEntry.Open();
                await using var destination = File.Create(destinationPath);
                await source.CopyToAsync(destination, ct);
            }
        }

        var entryScript = FindEntryScript(targetDir);
        var metadata = BuildArchiveMetadata(parsed, packageMetadata, slug, targetDir, entryScript);

        return (
            entry.FullName.Replace('\\', '/'),
            content,
            metadata is null ? null : JsonSerializer.Serialize(metadata));
    }

    private static bool ShouldSkipArchiveEntry(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return true;
        }

        var normalized = relativePath.Replace('\\', '/').Trim('/');
        return normalized.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("__MACOSX", StringComparison.OrdinalIgnoreCase)
            || normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.StartsWith("._", StringComparison.Ordinal));
    }

    private static void DeleteExtractedSkillDirectory(
        SandboxPathResolver pathResolver,
        string slug)
    {
        var targetDir = Path.Combine(pathResolver.GetSkillsHostDir(), "uploaded", slug);
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }
    }

    private static string? GetSingleTopLevelFolder(IEnumerable<ZipArchiveEntry> entries)
    {
        var rootSegments = entries
            .Select(item => item.FullName.Replace('\\', '/').Trim('/'))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Split('/', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length > 1)
            .Select(parts => parts[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return rootSegments.Length == 1 ? rootSegments[0] : null;
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
                return Path.GetRelativePath(targetDir, candidate);
            }
        }

        return Directory.Exists(Path.Combine(targetDir, "scripts"))
            ? Directory.GetFiles(Path.Combine(targetDir, "scripts"), "*.py", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetRelativePath(targetDir, path))
                .FirstOrDefault()
            : null;
    }

    private static string? MergeMetadataJson(
        string? existingMetadataJson,
        ParsedSkillDocument parsed)
    {
        var frontmatterMetadata = parsed.Metadata is null || parsed.Metadata.Count == 0
            ? null
            : parsed.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

        return MergeMetadataJson(existingMetadataJson, frontmatterMetadata, parsed);
    }

    private static string? MergeMetadataJson(
        string? existingMetadataJson,
        IDictionary<string, string?>? parsedMetadata,
        ParsedSkillDocument? parsed = null)
    {
        Dictionary<string, object?> merged = new(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(existingMetadataJson))
        {
            using var existing = JsonDocument.Parse(existingMetadataJson);
            if (existing.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in existing.RootElement.EnumerateObject())
                {
                    merged[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.Object => JsonSerializer.Deserialize<object>(property.Value.GetRawText()),
                        JsonValueKind.Array => property.Value.EnumerateArray().Select(item => item.ToString()).ToArray(),
                        _ => property.Value.ToString(),
                    };
                }
            }
        }

        if (parsedMetadata is not null)
        {
            foreach (var pair in parsedMetadata)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        merged["skill"] = BuildSkillMetadataObject(parsed, merged);

        return merged.Count == 0 ? null : JsonSerializer.Serialize(merged);
    }

    private static string ResolveSkillSlug(ParsedSkillDocument parsed, string? metadataJson)
    {
        if (!string.IsNullOrWhiteSpace(parsed.Slug))
        {
            return SkillMarkdownParser.BuildSlug(parsed.Slug);
        }

        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                using var document = JsonDocument.Parse(metadataJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("package", out var package) &&
                    package.ValueKind == JsonValueKind.Object &&
                    package.TryGetProperty("slug", out var slugProperty) &&
                    slugProperty.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(slugProperty.GetString()))
                {
                    return SkillMarkdownParser.BuildSlug(slugProperty.GetString()!);
                }
            }
            catch
            {
                // Ignore malformed metadata here; request validation handles it elsewhere.
            }
        }

        return SkillMarkdownParser.BuildSlug(parsed.Name);
    }

    private static string ResolveSkillSlug(ParsedSkillDocument parsed, ParsedSkillPackageMetadata? packageMetadata)
    {
        if (!string.IsNullOrWhiteSpace(parsed.Slug))
        {
            return SkillMarkdownParser.BuildSlug(parsed.Slug);
        }

        if (!string.IsNullOrWhiteSpace(packageMetadata?.Slug))
        {
            return SkillMarkdownParser.BuildSlug(packageMetadata.Slug);
        }

        return SkillMarkdownParser.BuildSlug(parsed.Name);
    }

    private static Dictionary<string, object?>? BuildArchiveMetadata(
        ParsedSkillDocument parsed,
        ParsedSkillPackageMetadata? packageMetadata,
        string slug,
        string targetDir,
        string? entryScript)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["skill"] = BuildSkillMetadataObject(
                parsed,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)),
        };

        if (packageMetadata is not null)
        {
            metadata["package"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ownerId"] = packageMetadata.OwnerId,
                ["slug"] = packageMetadata.Slug,
                ["version"] = packageMetadata.Version,
                ["publishedAt"] = packageMetadata.PublishedAt,
                ["raw"] = JsonSerializer.Deserialize<object>(packageMetadata.RawJson),
            };

            try
            {
                using var packageDocument = JsonDocument.Parse(packageMetadata.RawJson);
                if (packageDocument.RootElement.ValueKind == JsonValueKind.Object &&
                    packageDocument.RootElement.TryGetProperty("runtime", out var runtimeProperty) &&
                    runtimeProperty.ValueKind == JsonValueKind.Object)
                {
                    metadata["runtime"] = JsonSerializer.Deserialize<object>(runtimeProperty.GetRawText());
                }
            }
            catch
            {
                // Ignore malformed raw package metadata.
            }
        }

        if (entryScript is not null)
        {
            var runtimeVersion = parsed.Version ?? packageMetadata?.Version ?? "1.0.0";
            var runtime = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["type"] = "script",
                ["key"] = slug,
                ["name"] = slug,
                ["label"] = parsed.Name,
                ["version"] = runtimeVersion,
                ["hostPath"] = Path.Combine("uploaded", slug).Replace('\\', '/'),
                ["sandboxPath"] = $"/skills/uploaded/{slug}",
                ["entryScript"] = entryScript.Replace('\\', '/'),
                ["requirementsFile"] = FindRequirementsFile(targetDir),
                ["actions"] = new[] { "create_workbook" },
            };

            metadata["runtime"] = runtime;
            metadata["runtime_key"] = runtime["key"];
            metadata["runtime_name"] = runtime["name"];
            metadata["runtime_label"] = runtime["label"];
            metadata["runtime_version"] = runtime["version"];
            metadata["runtime_host_path"] = runtime["hostPath"];
            metadata["runtime_sandbox_path"] = runtime["sandboxPath"];
            metadata["runtime_entry_script"] = runtime["entryScript"];
            metadata["runtime_requirements_file"] = runtime["requirementsFile"];
            metadata["runtime_actions"] = runtime["actions"];
        }

        return metadata;
    }

    private static Dictionary<string, object?> BuildSkillMetadataObject(
        ParsedSkillDocument? parsed,
        IReadOnlyDictionary<string, object?> merged)
    {
        var existing = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (merged.TryGetValue("skill", out var skillObject) && skillObject is string skillJson && !string.IsNullOrWhiteSpace(skillJson))
        {
            try
            {
                using var document = JsonDocument.Parse(skillJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        existing[property.Name] = property.Value.ToString();
                    }
                }
            }
            catch
            {
                // Ignore legacy malformed skill metadata.
            }
        }

        if (parsed is null)
        {
            return existing;
        }

        existing["name"] = parsed.Name;
        existing["description"] = parsed.Description;
        existing["slug"] = parsed.Slug;
        existing["version"] = parsed.Version;
        existing["homepage"] = parsed.Homepage;
        existing["license"] = parsed.License;
        existing["compatibility"] = parsed.Compatibility;
        existing["allowedTools"] = parsed.AllowedTools;

        return existing;
    }

    private static string FindRequirementsFile(string targetDir)
    {
        var directPath = Path.Combine(targetDir, "requirements.txt");
        if (File.Exists(directPath))
        {
            return "requirements.txt";
        }

        var nestedPath = Directory.GetFiles(targetDir, "requirements.txt", SearchOption.AllDirectories)
            .OrderBy(path => path.Length)
            .FirstOrDefault();

        return nestedPath is null ? string.Empty : Path.GetRelativePath(targetDir, nestedPath).Replace('\\', '/');
    }

    private static async Task<string?> ValidateRequestAsync(
        string name,
        string slug,
        string fileName,
        string content,
        string status,
        string? metadataJson,
        ISkillRepo repo,
        Guid? existingId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Skill name is required.";
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return "Skill slug is required.";
        }

        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return "File name must end with .md.";
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return "Skill content is required.";
        }

        if (!IsValidStatus(status))
        {
            return "Status must be one of: active, paused, archived.";
        }

        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                JsonDocument.Parse(metadataJson);
            }
            catch
            {
                return "Metadata JSON is invalid.";
            }
        }

        try
        {
            SkillMarkdownParser.Parse(content);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        var duplicate = await repo.FindAsync(skill => skill.Slug == slug && (!existingId.HasValue || skill.Id != existingId.Value), ct);
        if (duplicate is not null)
        {
            return "Skill slug already exists.";
        }

        return null;
    }

    private static bool IsValidStatus(string status) =>
        status.Equals("active", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("paused", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("archived", StringComparison.OrdinalIgnoreCase);

    private static SkillResponse ToResponse(Skill skill) =>
        new(
            skill.Id.ToString(),
            skill.Name,
            skill.Slug,
            skill.Description,
            skill.FileName,
            skill.Content,
            skill.Status,
            skill.MetadataJson,
            skill.ArchiveFileName,
            skill.CreatedOn.ToString("O"),
            skill.UpdatedOn?.ToString("O"));
}
