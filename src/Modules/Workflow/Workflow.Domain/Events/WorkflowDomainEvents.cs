using BB.Common;

namespace Workflow.Domain.Events;

public sealed record IssueTransitioned(
    Guid IssueId,
    Guid WorkflowId,
    Guid? FromStatusId,
    Guid ToStatusId,
    Guid TransitionId,
    string ChangedBy) : DomainEvent;

public sealed record WorkflowCreated(Guid WorkflowId, Guid? ProjectId, string Key) : DomainEvent;

public sealed record WorkflowPublished(Guid WorkflowId, Guid? ProjectId) : DomainEvent;
