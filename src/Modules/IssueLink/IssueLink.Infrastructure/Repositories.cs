using BB.Persistence;
using IssueLink.Application.Repositories;
using IssueLink.Domain;
using Microsoft.EntityFrameworkCore;

namespace IssueLink.Infrastructure;

public sealed class IssueLinkRepository : Repository<Domain.IssueLink>, IIssueLinkRepository
{
    private readonly IssueLinkDbContext _ctx;
    public IssueLinkRepository(IssueLinkDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<IReadOnlyList<Domain.IssueLink>> ListBySourceAsync(Guid sourceIssueId, CancellationToken ct = default) =>
        QueryBy(l => l.SourceIssueId == sourceIssueId, ct);

    public Task<IReadOnlyList<Domain.IssueLink>> ListByTargetAsync(Guid targetIssueId, CancellationToken ct = default) =>
        QueryBy(l => l.TargetIssueId == targetIssueId, ct);

    public Task<bool> ExistsAsync(Guid sourceIssueId, Guid targetIssueId, IssueLinkType linkType, CancellationToken ct = default) =>
        _ctx.Links.AsNoTracking().AnyAsync(
            l => l.SourceIssueId == sourceIssueId
                 && l.TargetIssueId == targetIssueId
                 && l.LinkType == linkType,
            ct);

    private async Task<IReadOnlyList<Domain.IssueLink>> QueryBy(
        System.Linq.Expressions.Expression<Func<Domain.IssueLink, bool>> predicate,
        CancellationToken ct)
    {
        var list = await _ctx.Links.AsNoTracking()
            .Where(predicate)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
        return list;
    }
}

public sealed class IssueLinkUnitOfWork : UnitOfWork<IssueLinkDbContext>, IIssueLinkUnitOfWork
{
    public IssueLinkUnitOfWork(IssueLinkDbContext ctx) : base(ctx) { }
}
