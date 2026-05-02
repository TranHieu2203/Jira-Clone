using ActivityLog.Application.Repositories;
using ActivityLog.Domain;
using BB.Common;
using BB.Security;
using Comment.Domain.Events;
using Issue.Domain.Events;

namespace ActivityLog.Application.Handlers;

public sealed class IssueCreatedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueCreated>
{
    public IssueCreatedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueCreated e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueCreated,
            ResolveActor(e.ReporterId),
            new { issueKey = e.IssueKey, summary = e.Summary, issueTypeId = e.IssueTypeId, projectId = e.ProjectId }, ct);
}

public sealed class IssueUpdatedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueUpdated>
{
    public IssueUpdatedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueUpdated e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueFieldUpdated,
            ResolveActor(null),
            new { field = e.FieldName, oldValue = e.OldValue, newValue = e.NewValue }, ct);
}

public sealed class IssueAssigneeChangedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueAssigneeChanged>
{
    public IssueAssigneeChangedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueAssigneeChanged e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueAssigneeChanged,
            ResolveActor(null),
            new { oldAssigneeId = e.OldAssigneeId, newAssigneeId = e.NewAssigneeId }, ct);
}

public sealed class IssueStatusChangedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueStatusChanged>
{
    public IssueStatusChangedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueStatusChanged e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueStatusChanged,
            ResolveActor(null),
            new { fromStatusId = e.FromStatusId, toStatusId = e.ToStatusId, transitionId = e.TransitionId }, ct);
}

public sealed class IssueParentChangedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueParentChanged>
{
    public IssueParentChangedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueParentChanged e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueParentChanged,
            ResolveActor(null),
            new { oldParentId = e.OldParentId, newParentId = e.NewParentId }, ct);
}

public sealed class IssueWatcherAddedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueWatcherAdded>
{
    public IssueWatcherAddedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueWatcherAdded e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueWatcherAdded,
            ResolveActor(null),
            new { userId = e.UserId }, ct);
}

public sealed class IssueWatcherRemovedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueWatcherRemoved>
{
    public IssueWatcherRemovedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueWatcherRemoved e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueWatcherRemoved,
            ResolveActor(null),
            new { userId = e.UserId }, ct);
}

public sealed class IssueArchivedActivityHandler : ActivityHandlerBase, IDomainEventHandler<IssueArchived>
{
    public IssueArchivedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(IssueArchived e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.IssueArchived, ResolveActor(null), null, ct);
}

public sealed class CommentAddedActivityHandler : ActivityHandlerBase, IDomainEventHandler<CommentAdded>
{
    private const int BodyPreviewMax = 240;

    public CommentAddedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(CommentAdded e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.CommentAdded,
            ResolveActor(e.AuthorId),
            new { commentId = e.CommentId, bodyPreview = Truncate(e.Body, BodyPreviewMax) }, ct);

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

public sealed class CommentEditedActivityHandler : ActivityHandlerBase, IDomainEventHandler<CommentEdited>
{
    public CommentEditedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(CommentEdited e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.CommentEdited,
            ResolveActor(e.AuthorId),
            new { commentId = e.CommentId }, ct);
}

public sealed class CommentDeletedActivityHandler : ActivityHandlerBase, IDomainEventHandler<CommentDeleted>
{
    public CommentDeletedActivityHandler(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
        : base(repo, uow, currentUser) { }

    public Task HandleAsync(CommentDeleted e, CancellationToken ct = default) =>
        AppendAsync(e.IssueId, e.OccurredAt, ActivityKinds.CommentDeleted,
            ResolveActor(e.AuthorId),
            new { commentId = e.CommentId }, ct);
}
