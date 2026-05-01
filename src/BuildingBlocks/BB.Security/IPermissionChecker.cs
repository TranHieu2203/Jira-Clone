namespace BB.Security;

/// <summary>
/// Trừu tượng hoá kiểm tra quyền: org-level và project-level.
/// MVP impl dùng 4 role cố định (Owner / Admin / Member / Viewer).
/// Phase ≥ P11 swap sang impl dùng permission scheme đầy đủ mà không sửa caller.
/// </summary>
public interface IPermissionChecker
{
    Task<bool> HasOrgPermissionAsync(Guid userId, Guid orgId, string permission, CancellationToken ct = default);
    Task<bool> HasProjectPermissionAsync(Guid userId, Guid projectId, string permission, CancellationToken ct = default);
    Task<bool> IsInRoleAsync(Guid userId, Guid scopeId, string role, CancellationToken ct = default);
}

public static class PermissionKeys
{
    // Org
    public const string OrgManage = "org.manage";
    public const string OrgInviteMember = "org.invite_member";

    // Project
    public const string ProjectView = "project.view";
    public const string ProjectManage = "project.manage";
    public const string ProjectAdminWorkflow = "project.admin.workflow";
    public const string ProjectAdminField = "project.admin.field";

    // Issue
    public const string IssueCreate = "issue.create";
    public const string IssueEdit = "issue.edit";
    public const string IssueDelete = "issue.delete";
    public const string IssueTransition = "issue.transition";
    public const string IssueAssign = "issue.assign";
    public const string IssueComment = "issue.comment";
}

public static class Roles
{
    public const string OrgOwner = "ORG_OWNER";
    public const string ProjectAdmin = "PROJECT_ADMIN";
    public const string ProjectMember = "PROJECT_MEMBER";
    public const string ProjectViewer = "PROJECT_VIEWER";
}
