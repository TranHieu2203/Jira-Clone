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

    /// <summary>R6 throttle: có bản gửi thành công cho cùng (template, email) sau mốc <paramref name="since"/> không?</summary>
    Task<bool> ExistsRecentSentAsync(string templateKey, string toEmail, DateTimeOffset since, CancellationToken ct = default);
}

public interface IEmailUserPreferenceRepository : IRepository<EmailUserPreference>
{
    Task<EmailUserPreference?> FindByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, EmailUserPreference>> ListByUserIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
}

public interface INotificationUnitOfWork : IUnitOfWork { }
