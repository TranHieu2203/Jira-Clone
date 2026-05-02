using BB.Common;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Infrastructure;

public sealed class NotificationRepository : Repository<InAppNotification>, INotificationRepository
{
    private readonly NotificationDbContext _ctx;

    public NotificationRepository(NotificationDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<PagedList<InAppNotification>> ListForUserAsync(
        Guid userId, int pageIndex, int pageSize, bool unreadOnly, CancellationToken ct = default)
    {
        IQueryable<InAppNotification> q = _ctx.InAppNotifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        if (unreadOnly)
            q = q.Where(n => !n.IsRead);

        long total = await q.LongCountAsync(ct);
        int page = Math.Max(pageIndex, 1);
        int size = Math.Max(pageSize, 1);

        List<InAppNotification> items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedList<InAppNotification>(items, total, page, size);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default) =>
        _ctx.InAppNotifications.AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);

    public Task<InAppNotification?> GetAsync(Guid id, Guid recipientUserId, CancellationToken ct = default) =>
        _ctx.InAppNotifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == recipientUserId, ct);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        List<InAppNotification> list = await _ctx.InAppNotifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(ct);
        foreach (InAppNotification n in list)
            n.MarkRead();
    }
}

public sealed class NotificationUnitOfWork : UnitOfWork<NotificationDbContext>, INotificationUnitOfWork
{
    public NotificationUnitOfWork(NotificationDbContext ctx) : base(ctx) { }
}
