using BB.Common;

namespace Workflow.Domain;

public sealed class IssueStatusHistory : BaseEntity, IEntityWithTrace
{
    public Guid IssueId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public Guid? FromStatusId { get; private set; }
    public Guid ToStatusId { get; private set; }
    public Guid TransitionId { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; private set; }
    public string? Comment { get; private set; }
    public string? CreatedTraceId { get; set; }

    private IssueStatusHistory() { }

    public IssueStatusHistory(
        Guid issueId,
        Guid workflowId,
        Guid? fromStatusId,
        Guid toStatusId,
        Guid transitionId,
        string changedBy,
        DateTimeOffset changedAt,
        string? comment = null)
    {
        IssueId = issueId;
        WorkflowId = workflowId;
        FromStatusId = fromStatusId;
        ToStatusId = toStatusId;
        TransitionId = transitionId;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
        Comment = comment;
    }
}
