using System.Text.Json;
using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Notification.Application.Infrastructure;
using Notification.Application.Repositories;
using Notification.Domain;
using System.Linq;

namespace Notification.Application.Handlers;

public sealed class IssueStatusChangedNotificationHandler : IEventHandler<IssueStatusChangedIntegrationEvent>
{
    private readonly INotificationRepository _repo;
    private readonly INotificationUnitOfWork _uow;
    private readonly IEventEmailDispatcher _emails;
    private readonly ILogger<IssueStatusChangedNotificationHandler> _logger;

    public IssueStatusChangedNotificationHandler(
        INotificationRepository repo,
        INotificationUnitOfWork uow,
        IEventEmailDispatcher emails,
        ILogger<IssueStatusChangedNotificationHandler> logger)
    {
        _repo = repo;
        _uow = uow;
        _emails = emails;
        _logger = logger;
    }

    public async Task HandleAsync(IssueStatusChangedIntegrationEvent e, CancellationToken ct = default)
    {
        HashSet<Guid> targets = new();

        foreach (Guid w in e.WatcherUserIds)
        {
            if (w != e.ActorUserId)
                targets.Add(w);
        }

        if (e.AssigneeId is not null && e.AssigneeId != e.ActorUserId)
            targets.Add(e.AssigneeId.Value);

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
        _logger.LogInformation("Status change in-app for issue {Key} ({Count} recipients)", e.IssueKey, targets.Count);

        // Email: người xử lý (assignee) luôn nhận mail khi đổi status — kể cả tự đổi (actor == assignee),
        // vì inbox là bằng chứng ngoài app. In-app ở trên vẫn không spam chính actor nếu không có watcher khác.
        HashSet<Guid> emailTargets = new(targets);
        if (e.AssigneeId is not null)
            emailTargets.Add(e.AssigneeId.Value);

        await _emails.DispatchAsync(
            templateKey: "issue.status_changed",
            recipientUserIds: emailTargets.ToArray(),
            args: new Dictionary<string, string>
            {
                ["issueKey"] = e.IssueKey
            },
            ct: ct);
    }
}
