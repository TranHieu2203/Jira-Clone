using BB.Common;

namespace Notification.Application;

public interface INotificationService
{
    Task<Result<PagedList<InAppNotificationDto>>> ListMineAsync(int pageIndex, int pageSize, bool unreadOnly, CancellationToken ct = default);

    Task<Result<int>> UnreadCountAsync(CancellationToken ct = default);

    Task<Result> MarkReadAsync(Guid id, CancellationToken ct = default);

    Task<Result> MarkAllReadAsync(CancellationToken ct = default);
}
