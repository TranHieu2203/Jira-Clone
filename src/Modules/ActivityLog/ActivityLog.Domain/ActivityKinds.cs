namespace ActivityLog.Domain;

/// <summary>Stable keys aligned with FE i18n (<c>activity.*</c>).</summary>
public static class ActivityKinds
{
    public const string IssueCreated = "activity.issue.created";
    public const string IssueFieldUpdated = "activity.issue.field_updated";
    public const string IssueAssigneeChanged = "activity.issue.assignee_changed";
    public const string IssueStatusChanged = "activity.issue.status_changed";
    public const string IssueParentChanged = "activity.issue.parent_changed";
    public const string IssueWatcherAdded = "activity.issue.watcher_added";
    public const string IssueWatcherRemoved = "activity.issue.watcher_removed";
    public const string IssueArchived = "activity.issue.archived";
    public const string CommentAdded = "activity.comment.added";
    public const string CommentEdited = "activity.comment.edited";
    public const string CommentDeleted = "activity.comment.deleted";
}
