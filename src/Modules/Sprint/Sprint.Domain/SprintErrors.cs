namespace Sprint.Domain;

public static class SprintErrors
{
    public const string MsgNotFound = "sprint.not_found";
    public const string MsgNameRequired = "sprint.name_required";
    public const string MsgInvalidDates = "sprint.invalid_dates";
    public const string MsgCompletedImmutable = "sprint.completed_immutable";
    public const string MsgActiveExists = "sprint.active_exists";
    public const string MsgWrongStateStart = "sprint.start_requires_planned";
    public const string MsgWrongStateComplete = "sprint.complete_requires_active";
    public const string MsgIssueWrongProject = "sprint.issue_wrong_project";
    public const string MsgIssueInOtherSprint = "sprint.issue_in_other_sprint";
    public const string MsgIssueDuplicate = "sprint.issue_duplicate";
    public const string MsgBurndownRequiresActiveOrCompleted = "sprint.burndown_bad_state";
    public const string MsgEmptySprint = "sprint.start_requires_issues";
    public const string MsgReorderInvalid = "sprint.reorder_invalid";
    public const string MsgIssueNotInSprint = "sprint.issue_not_in_sprint";
}
