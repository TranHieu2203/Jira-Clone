using BB.Common;

namespace ActivityLog.Application;

public interface IActivityLogService
{
    Task<Result<PagedList<ActivityItemDto>>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default);
}
