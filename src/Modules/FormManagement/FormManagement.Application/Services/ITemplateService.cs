using BB.Common;
using FormManagement.Domain;

namespace FormManagement.Application.Services;

public interface ITemplateService
{
    Task<Result<IReadOnlyList<TemplateSummaryDto>>> SearchAsync(string? keyword, TemplateStatus? status, string? category, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> CreateAsync(CreateTemplateRequest request, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> UpdateMetadataAsync(Guid id, UpdateTemplateMetadataRequest request, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> UpdateContentAsync(Guid id, UpdateTemplateContentRequest request, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> PublishAsync(Guid id, CancellationToken ct = default);
    Task<Result<TemplateDetailDto>> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Parse DOCX để detect placeholder + trả về base64 cho FE state. Không persist template.</summary>
    Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default);

    /// <summary>Lấy raw DOCX bytes của template (OnlyOffice DocServer fetch qua endpoint này).</summary>
    Task<Result<byte[]>> GetDocxBytesAsync(Guid id, CancellationToken ct = default);

    /// <summary>OnlyOffice DocServer save callback: replace DOCX bytes + bump version.</summary>
    Task<Result> ReplaceDocxBytesAsync(Guid id, byte[] docxBytes, CancellationToken ct = default);

    /// <summary>
    /// Backfill: tìm templates còn lưu DocxBytes trong DB (legacy) → upload bytes lên S3 →
    /// set StorageKey → clear DocxBytes. Idempotent (template đã có StorageKey được skip).
    /// Trả về tuple { processed, skipped, failed } qua message args.
    /// </summary>
    Task<Result> BackfillToS3Async(CancellationToken ct = default);
}
