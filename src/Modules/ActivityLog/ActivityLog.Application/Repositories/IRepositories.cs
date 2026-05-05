using ActivityLog.Domain;
using BB.Common;
using BB.Persistence;

namespace ActivityLog.Application.Repositories;

public interface IActivityEntryRepository : IRepository<ActivityEntry>
{
    Task<PagedList<ActivityEntry>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default);

    /// <summary>Phục vụ burndown: đổi status của các issue trong khoảng thời gian.</summary>
    Task<IReadOnlyList<ActivityEntry>> ListIssueStatusChangesForIssuesAsync(
        IReadOnlyCollection<Guid> issueIds,
        DateTimeOffset fromUtcInclusive,
        DateTimeOffset toUtcInclusive,
        CancellationToken ct = default);
}

public interface IActivityLogUnitOfWork : IUnitOfWork;
