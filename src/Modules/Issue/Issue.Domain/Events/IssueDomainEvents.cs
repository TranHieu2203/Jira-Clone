using BB.Common;

namespace Issue.Domain.Events;

public sealed record IssueCreated(Guid IssueId, Guid ProjectId, Guid IssueTypeId, string IssueKey, string Summary, Guid ReporterId) : DomainEvent;
public sealed record IssueUpdated(Guid IssueId, string IssueKey, string FieldName, object? OldValue, object? NewValue) : DomainEvent;
public sealed record IssueAssigneeChanged(Guid IssueId, Guid? OldAssigneeId, Guid? NewAssigneeId) : DomainEvent;
public sealed record IssueStatusChanged(Guid IssueId, Guid FromStatusId, Guid ToStatusId, Guid TransitionId) : DomainEvent;
public sealed record IssueParentChanged(Guid IssueId, Guid? OldParentId, Guid? NewParentId) : DomainEvent;
public sealed record IssueWatcherAdded(Guid IssueId, Guid UserId) : DomainEvent;
public sealed record IssueWatcherRemoved(Guid IssueId, Guid UserId) : DomainEvent;
public sealed record IssueArchived(Guid IssueId) : DomainEvent;
