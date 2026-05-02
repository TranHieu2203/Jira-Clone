using BB.Common;
using BB.Security;
using Microsoft.Extensions.Logging;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Application;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly INotificationUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repo,
        INotificationUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<NotificationService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PagedList<InAppNotificationDto>>> ListMineAsync(
        int pageIndex, int pageSize, bool unreadOnly, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedList<InAppNotificationDto>>(ErrorType.Unauthorized, "auth.required");

        PagedList<InAppNotification> page = await _repo.ListForUserAsync(
            _currentUser.UserId.Value, pageIndex, pageSize, unreadOnly, ct);

        List<InAppNotificationDto> dtos = page.Items.Select(Mappers.ToDto).ToList();
        return Result.Success(new PagedList<InAppNotificationDto>(dtos, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<int>> UnreadCountAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<int>(ErrorType.Unauthorized, "auth.required");

        int n = await _repo.CountUnreadAsync(_currentUser.UserId.Value, ct);
        return Result.Success(n);
    }

    public async Task<Result> MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        InAppNotification? n = await _repo.GetAsync(id, _currentUser.UserId.Value, ct);
        if (n is null)
            return Result.Failure(ErrorType.NotFound, "notification.not_found");

        n.MarkRead();
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Notification {Id} marked read for user {User}", id, _currentUser.UserId);
        return Result.Success(messageKey: "notification.marked_read");
    }

    public async Task<Result> MarkAllReadAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        await _repo.MarkAllReadAsync(_currentUser.UserId.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "notification.all_marked_read");
    }
}
