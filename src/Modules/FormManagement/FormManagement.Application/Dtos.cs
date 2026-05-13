using FormManagement.Domain;

namespace FormManagement.Application;

// ============== Metadata ==============
public sealed record MetadataDto(
    Guid Id, string Value, string Label, MetadataType Type, string? FieldGroup,
    string? Description, string? ValidationJson, DateTimeOffset CreatedAt);

public sealed record CreateMetadataRequest(string Value, string Label, MetadataType Type, string? Description, string? ValidationJson);
public sealed record UpdateMetadataRequest(string Label, MetadataType Type, string? Description, string? ValidationJson);

// ============== Template ==============
public sealed record TemplateSummaryDto(
    Guid Id, string Code, string Name, string? Category,
    int Version, TemplateStatus Status, int UsedFieldsCount,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public sealed record TemplateDetailDto(
    Guid Id, string Code, string Name, string? Category,
    string SfdtContent, IReadOnlyList<string> UsedFields,
    int Version, TemplateStatus Status,
    bool HasOriginalDocx,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public sealed record CreateTemplateRequest(string Code, string Name, string? Category, string SfdtContent, IReadOnlyList<string>? UsedFields);
public sealed record UpdateTemplateMetadataRequest(string Name, string? Category);
public sealed record UpdateTemplateContentRequest(string SfdtContent, IReadOnlyList<string> UsedFields);

/// <summary>Kết quả import file Word → SFDT. Placeholders tự detect bằng regex từ form.md §2.</summary>
public sealed record TemplateImportResultDto(
    string SfdtContent,
    IReadOnlyList<DetectedPlaceholderDto> Placeholders);

public sealed record DetectedPlaceholderDto(string Text, string Pattern, int CharOffset);

// ============== Submission ==============
public sealed record SubmissionDto(
    Guid Id, Guid TemplateId, int TemplateVersion,
    ExportFormat ExportFormat, string? OutputPath, DateTimeOffset CreatedAt, string? CreatedBy);

public sealed record CreateSubmissionRequest(Guid TemplateId, IReadOnlyDictionary<string, object?> Data, ExportFormat ExportFormat);
