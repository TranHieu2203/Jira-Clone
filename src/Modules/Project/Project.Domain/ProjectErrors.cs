namespace Project.Domain;

public static class ProjectErrors
{
    // Workspace
    public const string WsNameRequired = "WORKSPACE_NAME_REQUIRED";
    public const string WsSlugInvalid = "WORKSPACE_SLUG_INVALID";
    public const string WsSlugDuplicated = "WORKSPACE_SLUG_DUPLICATED";
    public const string WsMemberDuplicated = "WORKSPACE_MEMBER_DUPLICATED";
    public const string WsMemberNotFound = "WORKSPACE_MEMBER_NOT_FOUND";
    public const string WsCannotRemoveOwner = "WORKSPACE_CANNOT_REMOVE_OWNER";

    // Project
    public const string ProjectNameRequired = "PROJECT_NAME_REQUIRED";
    public const string ProjectKeyInvalid = "PROJECT_KEY_INVALID";
    public const string ProjectKeyDuplicated = "PROJECT_KEY_DUPLICATED";
    public const string ProjectMemberDuplicated = "PROJECT_MEMBER_DUPLICATED";
    public const string ProjectMemberNotFound = "PROJECT_MEMBER_NOT_FOUND";
    public const string ProjectCannotRemoveLead = "PROJECT_CANNOT_REMOVE_LEAD";

    // IssueType
    public const string IssueTypeNameRequired = "ISSUE_TYPE_NAME_REQUIRED";
    public const string IssueTypeKeyInvalid = "ISSUE_TYPE_KEY_INVALID";
    public const string IssueTypeKeyDuplicated = "ISSUE_TYPE_KEY_DUPLICATED";
    public const string IssueTypeIsSystemCannotDelete = "ISSUE_TYPE_SYSTEM_CANNOT_DELETE";
    public const string IssueTypeNotFound = "ISSUE_TYPE_NOT_FOUND";

    // Messages (FE i18n keys)
    public const string MsgWsNameRequired = "workspace.name.required";
    public const string MsgWsSlugInvalid = "workspace.slug.invalid";
    public const string MsgWsSlugDup = "workspace.slug.duplicated";
    public const string MsgWsMemberDup = "workspace.member.duplicated";
    public const string MsgWsMemberNotFound = "workspace.member.not_found";
    public const string MsgWsCannotRemoveOwner = "workspace.cannot_remove_owner";

    public const string MsgProjectNameRequired = "project.name.required";
    public const string MsgProjectKeyInvalid = "project.key.invalid";
    public const string MsgProjectKeyDup = "project.key.duplicated";
    public const string MsgProjectMemberDup = "project.member.duplicated";
    public const string MsgProjectMemberNotFound = "project.member.not_found";
    public const string MsgProjectCannotRemoveLead = "project.cannot_remove_lead";

    public const string MsgIssueTypeNameRequired = "issue_type.name.required";
    public const string MsgIssueTypeKeyInvalid = "issue_type.key.invalid";
    public const string MsgIssueTypeKeyDup = "issue_type.key.duplicated";
    public const string MsgIssueTypeSystem = "issue_type.system_cannot_delete";
    public const string MsgIssueTypeNotFound = "issue_type.not_found";
}

public enum WorkspaceRole { Owner = 1, Admin = 2, Member = 3 }
public enum ProjectRole { Admin = 1, Member = 2, Viewer = 3 }
public enum ProjectType { Scrum = 1, Kanban = 2 }
