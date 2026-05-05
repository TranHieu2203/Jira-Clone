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

public sealed class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    private readonly NotificationDbContext _ctx;

    public EmailTemplateRepository(NotificationDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<EmailTemplate?> FindByKeyAsync(string key, CancellationToken ct = default) =>
        _ctx.EmailTemplates.FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task<PagedList<EmailTemplate>> ListAsync(int pageIndex, int pageSize, string? q, CancellationToken ct = default)
    {
        IQueryable<EmailTemplate> query = _ctx.EmailTemplates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            string s = q.Trim();
            query = query.Where(x => x.Key.Contains(s) || x.Name.Contains(s));
        }

        long total = await query.LongCountAsync(ct);
        int page = Math.Max(pageIndex, 1);
        int size = Math.Max(pageSize, 1);

        List<EmailTemplate> items = await query
            .OrderBy(x => x.Key)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedList<EmailTemplate>(items, total, page, size);
    }
}

public sealed class EmailLogRepository : Repository<EmailLog>, IEmailLogRepository
{
    private readonly NotificationDbContext _ctx;

    public EmailLogRepository(NotificationDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<PagedList<EmailLog>> ListAsync(
        int pageIndex, int pageSize, string? templateKey, string? toEmail, EmailLogStatus? status, CancellationToken ct = default)
    {
        IQueryable<EmailLog> q = _ctx.EmailLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(templateKey))
        {
            string key = templateKey.Trim();
            q = q.Where(x => x.TemplateKey == key);
        }
        if (!string.IsNullOrWhiteSpace(toEmail))
        {
            string to = toEmail.Trim();
            q = q.Where(x => x.ToEmail.Contains(to));
        }
        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        long total = await q.LongCountAsync(ct);
        int page = Math.Max(pageIndex, 1);
        int size = Math.Max(pageSize, 1);

        List<EmailLog> items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedList<EmailLog>(items, total, page, size);
    }
}

public sealed class NotificationUnitOfWork : UnitOfWork<NotificationDbContext>, INotificationUnitOfWork
{
    public NotificationUnitOfWork(NotificationDbContext ctx) : base(ctx) { }
}
