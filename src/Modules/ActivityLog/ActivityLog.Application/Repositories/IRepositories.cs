using ActivityLog.Domain;
using BB.Common;
using BB.Persistence;

namespace ActivityLog.Application.Repositories;

public interface IActivityEntryRepository : IRepository<ActivityEntry>
{
    Task<PagedList<ActivityEntry>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default);
}

public interface IActivityLogUnitOfWork : IUnitOfWork;
