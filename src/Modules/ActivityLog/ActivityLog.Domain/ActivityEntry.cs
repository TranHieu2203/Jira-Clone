using BB.Common;

namespace ActivityLog.Domain;

public sealed class ActivityEntry : AuditableEntity
{
    public Guid IssueId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public Guid? ActorUserId { get; private set; }
    public string? PayloadJson { get; private set; }

    private ActivityEntry() { }

    public ActivityEntry(Guid issueId, DateTimeOffset occurredAt, string kind, Guid? actorUserId, string? payloadJson)
    {
        IssueId = issueId;
        OccurredAt = occurredAt;
        Kind = kind;
        ActorUserId = actorUserId;
        PayloadJson = payloadJson;
    }
}
