using System.Text.Json;
using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Notification.Application.Infrastructure;
using Notification.Application.Repositories;
using Notification.Domain;
using System.Linq;

namespace Notification.Application.Handlers;

public sealed class CommentAddedNotificationHandler : IEventHandler<CommentAddedIntegrationEvent>
{
    private readonly INotificationRepository _repo;
    private readonly INotificationUnitOfWork _uow;
    private readonly IEventEmailDispatcher _emails;
    private readonly ILogger<CommentAddedNotificationHandler> _logger;

    public CommentAddedNotificationHandler(
        INotificationRepository repo,
        INotificationUnitOfWork uow,
        IEventEmailDispatcher emails,
        ILogger<CommentAddedNotificationHandler> logger)
    {
        _repo = repo;
        _uow = uow;
        _emails = emails;
        _logger = logger;
    }

    public async Task HandleAsync(CommentAddedIntegrationEvent e, CancellationToken ct = default)
    {
        HashSet<Guid> recipients = new();

        foreach (Guid w in e.WatcherUserIds)
        {
            if (w != e.AuthorUserId)
                recipients.Add(w);
        }

        foreach (Guid m in e.MentionUserIds)
            recipients.Add(m);

        if (e.AssigneeId is not null && e.AssigneeId != e.AuthorUserId)
            recipients.Add(e.AssigneeId.Value);

        recipients.Remove(e.AuthorUserId);

        foreach (Guid uid in recipients)
        {
            string payload = JsonSerializer.Serialize(new
            {
                issueId = e.IssueId,
                issueKey = e.IssueKey,
                projectId = e.ProjectId,
                commentId = e.CommentId,
                authorUserId = e.AuthorUserId,
                preview = e.BodyPreview
            });
            await _repo.AddAsync(new InAppNotification(uid, NotificationTypes.CommentAdded, payload), ct);
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Comment notifications for issue {Key} ({Count} recipients)", e.IssueKey, recipients.Count);

        await _emails.DispatchAsync(
            templateKey: "comment.added",
            recipientUserIds: recipients.ToArray(),
            args: new Dictionary<string, string>
            {
                ["issueKey"] = e.IssueKey,
                ["preview"] = e.BodyPreview
            },
            ct: ct);
    }
}
