using BB.Common;
using BB.Persistence;
using Issue.Domain;

namespace Issue.Application.Repositories;

public interface IIssueRepository : IRepository<Domain.Issue>
{
    Task<Domain.Issue?> GetWithWatchersAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Issue?> GetByKeyAsync(string issueKey, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(string issueKey, CancellationToken ct = default);
    Task<PagedList<Domain.Issue>> SearchAsync(IssueSearchCriteria criteria, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Issue>> ListByParentAsync(Guid parentIssueId, CancellationToken ct = default);
}

public sealed record IssueSearchCriteria(
    Guid? ProjectId,
    Guid? IssueTypeId,
    Guid? AssigneeId,
    Guid? ReporterId,
    Guid? CurrentStatusId,
    int? Priority,
    string? TextSearch,
    bool? IncludeArchived,
    int PageIndex = 1,
    int PageSize = 50,
    string? Sort = null,
    bool AssigneeUnassignedOnly = false,
    IReadOnlySet<Guid>? RestrictToIssueIds = null,
    IReadOnlySet<Guid>? ExcludeIssueIds = null,
    IReadOnlySet<Guid>? CurrentStatusIds = null);

public interface IIssueUnitOfWork : IUnitOfWork { }
