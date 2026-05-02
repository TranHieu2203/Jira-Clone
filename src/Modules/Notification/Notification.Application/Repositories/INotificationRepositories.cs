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

public interface INotificationUnitOfWork : IUnitOfWork { }
