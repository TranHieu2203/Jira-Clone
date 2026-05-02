using BB.Common;
using BB.Persistence;
using BB.Persistence.Specification;
using Issue.Application.Repositories;
using Issue.Domain;
using Microsoft.EntityFrameworkCore;

namespace Issue.Infrastructure;

public sealed class IssueRepository : Repository<Domain.Issue>, IIssueRepository
{
    private readonly IssueDbContext _ctx;
    public IssueRepository(IssueDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<Domain.Issue?> GetWithWatchersAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Issues.Include(i => i.Watchers).FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Domain.Issue?> GetByKeyAsync(string issueKey, CancellationToken ct = default) =>
        _ctx.Issues.Include(i => i.Watchers).FirstOrDefaultAsync(i => i.Key == issueKey, ct);

    public Task<bool> KeyExistsAsync(string issueKey, CancellationToken ct = default) =>
        _ctx.Issues.AsNoTracking().AnyAsync(i => i.Key == issueKey, ct);

    public async Task<PagedList<Domain.Issue>> SearchAsync(IssueSearchCriteria criteria, CancellationToken ct = default)
    {
        ISpecification<Domain.Issue> spec = IssueSpecifications.From(criteria);
        IQueryable<Domain.Issue> q = _ctx.Issues.AsNoTracking().Where(spec.Criteria);

        var total = await q.LongCountAsync(ct);
        var page = Math.Max(criteria.PageIndex, 1);
        var size = Math.Max(criteria.PageSize, 1);

        // Sort: mặc định CreatedAt desc; cho phép "key", "priority", "summary"
        q = (criteria.Sort?.ToLowerInvariant()) switch
        {
            "key" => q.OrderBy(i => i.Key),
            "priority" => q.OrderByDescending(i => i.Priority).ThenByDescending(i => i.CreatedAt),
            "summary" => q.OrderBy(i => i.Summary),
            _ => q.OrderByDescending(i => i.CreatedAt)
        };

        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedList<Domain.Issue>(items, total, page, size);
    }

    public async Task<IReadOnlyList<Domain.Issue>> ListByParentAsync(Guid parentIssueId, CancellationToken ct = default) =>
        await _ctx.Issues.AsNoTracking()
            .Where(i => i.ParentIssueId == parentIssueId)
            .OrderBy(i => i.Number)
            .ToListAsync(ct);
}

public sealed class IssueUnitOfWork : UnitOfWork<IssueDbContext>, IIssueUnitOfWork
{
    public IssueUnitOfWork(IssueDbContext ctx) : base(ctx) { }
}
