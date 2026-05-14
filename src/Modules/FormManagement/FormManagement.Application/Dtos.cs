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
    IReadOnlyList<string> UsedFields,
    int Version, TemplateStatus Status,
    bool HasOriginalDocx,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

/// <summary>
/// Tạo template từ DOCX upload. <c>DocxBase64</c> là DOCX gốc — BE persist vào
/// <c>DocumentTemplate.DocxBytes</c> để OnlyOffice Connector fetch + mail-merge giữ format.
/// </summary>
public sealed record CreateTemplateRequest(
    string Code,
    string Name,
    string? Category,
    string DocxBase64,
    IReadOnlyList<string>? UsedFields);
public sealed record UpdateTemplateMetadataRequest(string Name, string? Category);
public sealed record UpdateTemplateContentRequest(string DocxBase64, IReadOnlyList<string> UsedFields);

/// <summary>
/// Kết quả import file DOCX. Placeholders detect qua regex form.md §2. DocxBase64 trả về cho
/// FE giữ trong state (Phase OnlyOffice không cần SFDT conversion — render DOCX native).
/// </summary>
public sealed record TemplateImportResultDto(
    IReadOnlyList<DetectedPlaceholderDto> Placeholders,
    string DocxBase64);

public sealed record DetectedPlaceholderDto(string Text, string Pattern, int CharOffset);

// ============== Submission ==============
public sealed record SubmissionDto(
    Guid Id, Guid TemplateId, int TemplateVersion,
    ExportFormat ExportFormat, string? OutputPath, DateTimeOffset CreatedAt, string? CreatedBy);

public sealed record CreateSubmissionRequest(Guid TemplateId, IReadOnlyDictionary<string, object?> Data, ExportFormat ExportFormat);
