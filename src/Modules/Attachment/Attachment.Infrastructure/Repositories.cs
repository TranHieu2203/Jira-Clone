using Attachment.Application.Repositories;
using Attachment.Domain;
using BB.Common;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attachment.Infrastructure;

public sealed class AttachmentRepository : Repository<IssueAttachment>, IAttachmentRepository
{
    private readonly AttachmentDbContext _ctx;

    public AttachmentRepository(AttachmentDbContext ctx)
        : base(ctx) =>
        _ctx = ctx;

    public async Task<PagedList<IssueAttachment>> ListByIssueAsync(
        Guid issueId,
        int pageIndex,
        int pageSize,
        CancellationToken ct = default)
    {
        IQueryable<IssueAttachment> q = _ctx.Attachments.AsNoTracking().Where(a => a.IssueId == issueId);
        long total = await q.LongCountAsync(ct);
        int page = Math.Max(pageIndex, 1);
        int size = Math.Max(pageSize, 1);
        List<IssueAttachment> items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);
        return new PagedList<IssueAttachment>(items, total, page, size);
    }

    public Task<IssueAttachment?> GetByIdAndIssueAsync(Guid issueId, Guid attachmentId, CancellationToken ct = default) =>
        _ctx.Attachments.FirstOrDefaultAsync(a => a.IssueId == issueId && a.Id == attachmentId, ct);
}

public sealed class AttachmentUnitOfWork : UnitOfWork<AttachmentDbContext>, IAttachmentUnitOfWork
{
    public AttachmentUnitOfWork(AttachmentDbContext ctx)
        : base(ctx)
    {
    }
}
