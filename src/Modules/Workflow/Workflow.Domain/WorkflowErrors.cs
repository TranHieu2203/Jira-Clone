namespace Workflow.Domain;

/// <summary>
/// Error code + message key cho domain Workflow.
/// Code: SCREAMING_SNAKE_CASE — dùng cho client logic.
/// MessageKey: dot.case — FE translate qua i18n.
/// </summary>
public static class WorkflowErrors
{
    public const string NameRequired = "WORKFLOW_NAME_REQUIRED";
    public const string KeyRequired = "WORKFLOW_KEY_REQUIRED";
    public const string KeyInvalid = "WORKFLOW_KEY_INVALID";
    public const string MustHaveInitialStatus = "WORKFLOW_MUST_HAVE_INITIAL_STATUS";
    public const string StatusNotFound = "WORKFLOW_STATUS_NOT_FOUND";
    public const string StatusInUse = "WORKFLOW_STATUS_IN_USE";
    public const string StatusKeyDuplicated = "WORKFLOW_STATUS_KEY_DUPLICATED";
    public const string TransitionNotFound = "WORKFLOW_TRANSITION_NOT_FOUND";
    public const string TransitionInvalid = "WORKFLOW_TRANSITION_INVALID";
    public const string TransitionDuplicated = "WORKFLOW_TRANSITION_DUPLICATED";
    public const string IssueStatusNotInWorkflow = "WORKFLOW_ISSUE_STATUS_NOT_IN_WORKFLOW";

    public const string MsgNameRequired = "workflow.name.required";
    public const string MsgKeyRequired = "workflow.key.required";
    public const string MsgKeyInvalid = "workflow.key.invalid";
    public const string MsgMustHaveInitial = "workflow.must_have_initial_status";
    public const string MsgStatusNotFound = "workflow.status.not_found";
    public const string MsgStatusInUse = "workflow.status.in_use";
    public const string MsgStatusKeyDup = "workflow.status.key_duplicated";
    public const string MsgTransitionNotFound = "workflow.transition.not_found";
    public const string MsgTransitionInvalid = "workflow.transition.invalid";
    public const string MsgTransitionDup = "workflow.transition.duplicated";
}
