using System.Text.Json;
using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Notification.Application.Infrastructure;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Application.Handlers;

public sealed class IssueAssigneeChangedNotificationHandler : IEventHandler<IssueAssigneeChangedIntegrationEvent>
{
    private readonly INotificationRepository _repo;
    private readonly INotificationUnitOfWork _uow;
    private readonly IEventEmailDispatcher _emails;
    private readonly ILogger<IssueAssigneeChangedNotificationHandler> _logger;

    public IssueAssigneeChangedNotificationHandler(
        INotificationRepository repo,
        INotificationUnitOfWork uow,
        IEventEmailDispatcher emails,
        ILogger<IssueAssigneeChangedNotificationHandler> logger)
    {
        _repo = repo;
        _uow = uow;
        _emails = emails;
        _logger = logger;
    }

    public async Task HandleAsync(IssueAssigneeChangedIntegrationEvent e, CancellationToken ct = default)
    {
        if (e.NewAssigneeId is null || e.NewAssigneeId == e.ActorUserId)
            return;

        Guid recipient = e.NewAssigneeId.Value;
        string payload = JsonSerializer.Serialize(new
        {
            issueId = e.IssueId,
            issueKey = e.IssueKey,
            projectId = e.ProjectId,
            previousAssigneeId = e.PreviousAssigneeId,
            actorUserId = e.ActorUserId
        });

        await _repo.AddAsync(new InAppNotification(recipient, NotificationTypes.AssigneeChanged, payload), ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Notify assignee {UserId} for issue {Key}", recipient, e.IssueKey);

        await _emails.DispatchAsync(
            templateKey: "issue.assignee_changed",
            recipientUserIds: new[] { recipient },
            args: new Dictionary<string, string>
            {
                ["issueKey"] = e.IssueKey
            },
            ct: ct);
    }
}
