using AuditLog.Application.Repositories;
using AuditLog.Domain;
using BB.Common;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditLog.Infrastructure;

public sealed class AuditEntryRepository : Repository<AuditEntry>, IAuditEntryRepository
{
    private readonly AuditLogDbContext _ctx;
    public AuditEntryRepository(AuditLogDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<PagedList<AuditEntry>> SearchAsync(SearchAuditCriteria criteria, CancellationToken ct = default)
    {
        IQueryable<AuditEntry> q = _ctx.Entries.AsNoTracking();

        if (criteria.ActorUserId.HasValue) q = q.Where(e => e.ActorUserId == criteria.ActorUserId);
        if (criteria.Action is { Length: > 0 }) q = q.Where(e => e.Action == criteria.Action);
        if (criteria.Scope is { Length: > 0 }) q = q.Where(e => e.Scope == criteria.Scope);
        if (criteria.ScopeId.HasValue) q = q.Where(e => e.ScopeId == criteria.ScopeId);
        if (criteria.From.HasValue) q = q.Where(e => e.OccurredAt >= criteria.From);
        if (criteria.To.HasValue) q = q.Where(e => e.OccurredAt <= criteria.To);

        long total = await q.LongCountAsync(ct);
        int page = Math.Max(criteria.PageIndex, 1);
        int size = Math.Clamp(criteria.PageSize, 1, 200);

        List<AuditEntry> items = await q
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        return new PagedList<AuditEntry>(items, total, page, size);
    }
}

public sealed class AuditUnitOfWork : UnitOfWork<AuditLogDbContext>, IAuditUnitOfWork
{
    public AuditUnitOfWork(AuditLogDbContext ctx) : base(ctx) { }
}
