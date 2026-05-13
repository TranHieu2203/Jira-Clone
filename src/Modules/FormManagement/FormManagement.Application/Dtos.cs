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

/// <summary>
/// Tạo template. <c>DocxBase64</c> là DOCX gốc (nếu user import từ Word) — BE decode + persist
/// vào <c>DocumentTemplate.DocxBytes</c> để mail-merge giữ nguyên watermark/formatting; SFDT chỉ
/// dùng để editor render và bị strip watermark/VML.
/// </summary>
public sealed record CreateTemplateRequest(
    string Code,
    string Name,
    string? Category,
    string SfdtContent,
    IReadOnlyList<string>? UsedFields,
    string? DocxBase64 = null);
public sealed record UpdateTemplateMetadataRequest(string Name, string? Category);
public sealed record UpdateTemplateContentRequest(string SfdtContent, IReadOnlyList<string> UsedFields);

/// <summary>
/// Kết quả import file Word → SFDT. Placeholders tự detect bằng regex từ form.md §2.
/// <para><c>DocxBase64</c>: nguyên DOCX gốc encoded — FE lưu vào component state, gửi kèm khi save
/// template để BE mail-merge giữ nguyên watermark/formatting mà SFDT bị strip.</para>
/// <para><c>WatermarkText</c>: text watermark extracted từ DocIO (nếu có) — FE render CSS overlay
/// lên editor canvas vì Syncfusion DocumentEditor v33 không hiển thị watermark.</para>
/// </summary>
public sealed record TemplateImportResultDto(
    string SfdtContent,
    IReadOnlyList<DetectedPlaceholderDto> Placeholders,
    string? DocxBase64 = null,
    string? WatermarkText = null);

public sealed record DetectedPlaceholderDto(string Text, string Pattern, int CharOffset);

// ============== Submission ==============
public sealed record SubmissionDto(
    Guid Id, Guid TemplateId, int TemplateVersion,
    ExportFormat ExportFormat, string? OutputPath, DateTimeOffset CreatedAt, string? CreatedBy);

public sealed record CreateSubmissionRequest(Guid TemplateId, IReadOnlyDictionary<string, object?> Data, ExportFormat ExportFormat);
