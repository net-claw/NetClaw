using System.Text.Json.Serialization;

namespace NetClaw.Contracts.Llm.Skills;

public record GetSkillsRequest(
    int? PageIndex,
    int? PageSize,
    string? SearchText,
    string? OrderBy,
    bool? Ascending,
    string? Status);

public record CreateSkillRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("file_name")] string File_Name,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("metadata_json")] string? Metadata_Json);

public record UpdateSkillRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("file_name")] string File_Name,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("metadata_json")] string? Metadata_Json);

public record SkillResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("file_name")] string File_Name,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("metadata_json")] string? Metadata_Json,
    [property: JsonPropertyName("archive_file_name")] string? Archive_File_Name,
    [property: JsonPropertyName("created_at")] string Created_At,
    [property: JsonPropertyName("updated_at")] string? Updated_At);
