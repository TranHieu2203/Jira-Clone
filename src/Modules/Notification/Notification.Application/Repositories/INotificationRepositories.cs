using BB.Common;
using BB.Persistence;
using Notification.Domain;

namespace Notification.Application.Repositories;

public interface INotificationRepository : IRepository<InAppNotification>
{
    Task<PagedList<InAppNotification>> ListForUserAsync(Guid userId, int pageIndex, int pageSize, bool unreadOnly, CancellationToken ct = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);

    Task<InAppNotification?> GetAsync(Guid id, Guid recipientUserId, CancellationToken ct = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> FindByKeyAsync(string key, CancellationToken ct = default);
    Task<PagedList<EmailTemplate>> ListAsync(int pageIndex, int pageSize, string? q, CancellationToken ct = default);
}

public interface IEmailLogRepository : IRepository<EmailLog>
{
    Task<PagedList<EmailLog>> ListAsync(int pageIndex, int pageSize, string? templateKey, string? toEmail, EmailLogStatus? status, CancellationToken ct = default);
}

public interface INotificationUnitOfWork : IUnitOfWork { }
