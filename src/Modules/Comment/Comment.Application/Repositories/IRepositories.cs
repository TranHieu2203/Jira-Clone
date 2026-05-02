using BB.Common;
using BB.Persistence;
using Comment.Domain;

namespace Comment.Application.Repositories;

public interface ICommentRepository : IRepository<Domain.Comment>
{
    Task<PagedList<Domain.Comment>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default);
    Task<int> CountByIssueAsync(Guid issueId, CancellationToken ct = default);
}

public interface ICommentUnitOfWork : IUnitOfWork { }
