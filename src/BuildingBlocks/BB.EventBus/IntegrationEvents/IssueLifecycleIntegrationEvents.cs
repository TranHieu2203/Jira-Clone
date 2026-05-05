namespace BB.EventBus.IntegrationEvents;

/// <summary>Integration events cross-module — serialized vào outbox.</summary>
public sealed record IssueAssigneeChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string? TraceId,
    Guid IssueId,
    string IssueKey,
    Guid ProjectId,
    Guid? PreviousAssigneeId,
    Guid? NewAssigneeId,
    Guid? ActorUserId,
    List<Guid> WatcherUserIds) : IntegrationEvent(EventId, OccurredAt, TraceId);

public sealed record IssueStatusChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string? TraceId,
    Guid IssueId,
    string IssueKey,
    Guid ProjectId,
    Guid? AssigneeId,
    Guid FromStatusId,
    Guid ToStatusId,
    Guid? ActorUserId,
    List<Guid> WatcherUserIds) : IntegrationEvent(EventId, OccurredAt, TraceId);

public sealed record CommentAddedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string? TraceId,
    Guid IssueId,
    string IssueKey,
    Guid ProjectId,
    Guid? AssigneeId,
    Guid CommentId,
    Guid AuthorUserId,
    string BodyPreview,
    List<Guid> MentionUserIds,
    List<Guid> WatcherUserIds) : IntegrationEvent(EventId, OccurredAt, TraceId);
