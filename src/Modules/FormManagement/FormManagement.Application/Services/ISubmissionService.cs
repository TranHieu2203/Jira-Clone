using BB.Common;

namespace FormManagement.Application.Services;

public interface ISubmissionService
{
    Task<Result<IReadOnlyList<SubmissionDto>>> ListByTemplateAsync(Guid templateId, CancellationToken ct = default);
    Task<Result<SubmissionDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Tạo submission + mail-merge → output bytes. Caller (controller) sẽ stream về client.</summary>
    Task<Result<SubmissionExportDto>> SubmitAndExportAsync(CreateSubmissionRequest request, CancellationToken ct = default);
}

public sealed record SubmissionExportDto(SubmissionDto Submission, byte[] FileBytes, string FileName, string ContentType);
