using BB.Common;

namespace Attachment.Application;

public interface IAttachmentService
{
    Task<Result<PagedList<AttachmentDto>>> ListByIssueAsync(
        Guid issueId,
        int pageIndex = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<Result<AttachmentDto>> UploadAsync(
        Guid issueId,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default);

    Task<Result<AttachmentDownload>> OpenDownloadAsync(
        Guid issueId,
        Guid attachmentId,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid issueId, Guid attachmentId, CancellationToken ct = default);
}
