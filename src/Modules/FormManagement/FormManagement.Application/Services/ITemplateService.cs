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

    /// <summary>Import .docx / Word XML qua DocIO → SFDT + danh sách placeholder detected.</summary>
    Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default);
}
