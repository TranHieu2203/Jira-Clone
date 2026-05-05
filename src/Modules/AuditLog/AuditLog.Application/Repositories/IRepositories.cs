using AuditLog.Domain;
using BB.Common;
using BB.Persistence;

namespace AuditLog.Application.Repositories;

public interface IAuditEntryRepository : IRepository<AuditEntry>
{
    Task<PagedList<AuditEntry>> SearchAsync(SearchAuditCriteria criteria, CancellationToken ct = default);
}

public sealed record SearchAuditCriteria(
    Guid? ActorUserId,
    string? Action,
    string? Scope,
    Guid? ScopeId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageIndex,
    int PageSize);

public interface IAuditUnitOfWork : IUnitOfWork { }
