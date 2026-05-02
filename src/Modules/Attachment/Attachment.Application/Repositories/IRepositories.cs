using Attachment.Domain;
using BB.Common;
using BB.Persistence;

namespace Attachment.Application.Repositories;

public interface IAttachmentRepository : IRepository<IssueAttachment>
{
    Task<PagedList<IssueAttachment>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default);

    Task<IssueAttachment?> GetByIdAndIssueAsync(Guid issueId, Guid attachmentId, CancellationToken ct = default);
}

public interface IAttachmentUnitOfWork : IUnitOfWork;
