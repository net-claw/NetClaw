namespace NetClaw.Contracts.Llm.Skills;

public record GetSkillsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Status);

public record CreateSkillRequest(
    string Name,
    string Slug,
    string Description,
    string FileName,
    string Content,
    string Status,
    string? MetadataJson);

public record UpdateSkillRequest(
    string Name,
    string Slug,
    string Description,
    string FileName,
    string Content,
    string Status,
    string? MetadataJson);

public record SkillResponse(
    string Id,
    string Name,
    string Slug,
    string Description,
    string FileName,
    string Content,
    string Status,
    string? MetadataJson,
    string? ArchiveFileName,
    string CreatedAt,
    string? UpdatedAt);
