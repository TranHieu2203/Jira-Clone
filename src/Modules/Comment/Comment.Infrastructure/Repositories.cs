using BB.Common;
using BB.Persistence;
using Comment.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Comment.Infrastructure;

public sealed class CommentRepository : Repository<Domain.Comment>, ICommentRepository
{
    private readonly CommentDbContext _ctx;
    public CommentRepository(CommentDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<PagedList<Domain.Comment>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var q = _ctx.Comments.AsNoTracking().Where(c => c.IssueId == issueId);
        var total = await q.LongCountAsync(ct);
        var page = Math.Max(pageIndex, 1);
        var size = Math.Max(pageSize, 1);
        var items = await q
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);
        return new PagedList<Domain.Comment>(items, total, page, size);
    }

    public Task<int> CountByIssueAsync(Guid issueId, CancellationToken ct = default) =>
        _ctx.Comments.AsNoTracking().Where(c => c.IssueId == issueId).CountAsync(ct);
}

public sealed class CommentUnitOfWork : UnitOfWork<CommentDbContext>, ICommentUnitOfWork
{
    public CommentUnitOfWork(CommentDbContext ctx) : base(ctx) { }
}
