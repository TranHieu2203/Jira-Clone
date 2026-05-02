using ActivityLog.Application.Repositories;
using ActivityLog.Domain;
using BB.Common;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ActivityLog.Infrastructure;

public sealed class ActivityEntryRepository : Repository<ActivityEntry>, IActivityEntryRepository
{
    private readonly ActivityLogDbContext _ctx;

    public ActivityEntryRepository(ActivityLogDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<PagedList<ActivityEntry>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<ActivityEntry> q = _ctx.ActivityEntries.AsNoTracking().Where(a => a.IssueId == issueId);
        long total = await q.LongCountAsync(ct);
        int page = Math.Max(pageIndex, 1);
        int size = Math.Max(pageSize, 1);
        List<ActivityEntry> items = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);
        return new PagedList<ActivityEntry>(items, total, page, size);
    }
}

public sealed class ActivityLogUnitOfWork : UnitOfWork<ActivityLogDbContext>, IActivityLogUnitOfWork
{
    public ActivityLogUnitOfWork(ActivityLogDbContext ctx) : base(ctx) { }
}
