using BB.Common;

namespace AuditLog.Application;

public interface IAuditQueryService
{
    Task<Result<PagedList<AuditEntryDto>>> SearchAsync(SearchAuditRequest request, CancellationToken ct = default);
}
