using System.Text.Json;
using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Application.Handlers;

public sealed class IssueStatusChangedNotificationHandler : IEventHandler<IssueStatusChangedIntegrationEvent>
{
    private readonly INotificationRepository _repo;
    private readonly INotificationUnitOfWork _uow;
    private readonly ILogger<IssueStatusChangedNotificationHandler> _logger;

    public IssueStatusChangedNotificationHandler(
        INotificationRepository repo,
        INotificationUnitOfWork uow,
        ILogger<IssueStatusChangedNotificationHandler> logger)
    {
        _repo = repo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(IssueStatusChangedIntegrationEvent e, CancellationToken ct = default)
    {
        IEnumerable<Guid> targets = e.WatcherUserIds
            .Where(u => u != e.ActorUserId)
            .Distinct();

        foreach (Guid uid in targets)
        {
            string payload = JsonSerializer.Serialize(new
            {
                issueId = e.IssueId,
                issueKey = e.IssueKey,
                projectId = e.ProjectId,
                fromStatusId = e.FromStatusId,
                toStatusId = e.ToStatusId,
                actorUserId = e.ActorUserId
            });
            await _repo.AddAsync(new InAppNotification(uid, NotificationTypes.StatusChanged, payload), ct);
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Status change notifications for issue {Key} ({Count} recipients)", e.IssueKey, e.WatcherUserIds.Count);
    }
}
