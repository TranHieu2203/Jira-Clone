namespace Issue.Domain;

public static class IssueErrors
{
    public const string SummaryRequired = "ISSUE_SUMMARY_REQUIRED";
    public const string SummaryTooLong = "ISSUE_SUMMARY_TOO_LONG";
    public const string KeyInvalid = "ISSUE_KEY_INVALID";
    public const string ParentSelf = "ISSUE_PARENT_SELF";
    public const string WatcherDuplicated = "ISSUE_WATCHER_DUPLICATED";
    public const string WatcherNotFound = "ISSUE_WATCHER_NOT_FOUND";
    public const string EstimateNegative = "ISSUE_ESTIMATE_NEGATIVE";
    public const string AlreadyArchived = "ISSUE_ALREADY_ARCHIVED";
    public const string NotArchived = "ISSUE_NOT_ARCHIVED";

    public const string MsgSummaryRequired = "issue.summary.required";
    public const string MsgSummaryTooLong = "issue.summary.too_long";
    public const string MsgKeyInvalid = "issue.key.invalid";
    public const string MsgParentSelf = "issue.parent.self";
    public const string MsgWatcherDup = "issue.watcher.duplicated";
    public const string MsgWatcherNotFound = "issue.watcher.not_found";
    public const string MsgEstimateNegative = "issue.estimate.negative";
    public const string MsgAlreadyArchived = "issue.already_archived";
    public const string MsgNotArchived = "issue.not_archived";
}
