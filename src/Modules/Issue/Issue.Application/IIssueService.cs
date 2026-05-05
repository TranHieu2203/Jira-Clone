using BB.Common;

namespace Issue.Application;

public interface IIssueService
{
    Task<Result<IssueDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IssueDto>> GetByKeyAsync(string issueKey, CancellationToken ct = default);
    Task<Result<PagedList<IssueSummaryDto>>> SearchAsync(SearchIssuesRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IssueSummaryDto>>> ListChildrenAsync(Guid parentIssueId, CancellationToken ct = default);

    Task<Result<IssueDto>> CreateAsync(CreateIssueRequest request, CancellationToken ct = default);
    Task<Result<IssueDto>> UpdateAsync(Guid id, UpdateIssueRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<Result> UnarchiveAsync(Guid id, CancellationToken ct = default);

    Task<Result<IssueDto>> TransitionAsync(Guid id, TransitionIssueRequest request, CancellationToken ct = default);

    Task<Result<IssueDto>> AddWatcherAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<IssueDto>> RemoveWatcherAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>F5: bulk update — applies same operations to many issues, partial-success aware.</summary>
    Task<Result<BulkUpdateResultDto>> BulkUpdateAsync(BulkUpdateRequest request, CancellationToken ct = default);

    /// <summary>
    /// F8: export search results as CSV (capped at 5000 rows). Output: UTF-8 with BOM.
    /// Resolves Type Name + Status Name per-project so file is usable in Excel without
    /// manual ID joining. Reuses access/permission rules from SearchAsync.
    /// </summary>
    Task<Result<string>> ExportSearchAsCsvAsync(SearchIssuesRequest request, CancellationToken ct = default);
}
