using BB.Common;

namespace Issue.Domain;

public sealed class IssueWatcher : BaseEntity
{
    public Guid IssueId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private IssueWatcher() { }

    internal IssueWatcher(Guid issueId, Guid userId, DateTimeOffset addedAt)
    {
        IssueId = issueId;
        UserId = userId;
        AddedAt = addedAt;
    }
}
