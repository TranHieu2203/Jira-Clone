namespace BB.Security;

/// <summary>
/// Cross-cutting audit logger cho admin / security-significant actions.
/// Khác <c>ActivityLog</c> (per-issue user activity); audit chỉ ghi action ở level
/// admin/org/project (create/delete project, member change, workflow CRUD…).
///
/// Caller (service) gọi trực tiếp <see cref="LogAsync"/> sau khi action thành công.
/// Implementation thực tế ở <c>AuditLog.Infrastructure</c>; dev/test có NoOp impl.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Ghi 1 audit entry. Best-effort — không throw nếu DB lỗi (audit không nên block business action).
    /// </summary>
    /// <param name="action">Hằng số `AuditActions.*` (vd `project.created`, `workspace.member.added`).</param>
    /// <param name="scope">Loại scope: `org` | `project` | `workflow` | `custom_field` | `email_template` …</param>
    /// <param name="scopeId">Id của scope (workspaceId / projectId / workflowId…). Null nếu global.</param>
    /// <param name="payload">Object serialize sang JSON để lưu chi tiết — tránh PII (password, tokens).</param>
    Task LogAsync(string action, string scope, Guid? scopeId, object? payload, CancellationToken ct = default);
}

/// <summary>
/// Hằng số action key. Format: `<scope>.<verb>` để dễ filter sau này.
/// Khi thêm action mới → thêm vào đây + bảo đảm không trùng (case-sensitive).
/// </summary>
public static class AuditActions
{
    // Workspace / org
    public const string WorkspaceCreated = "workspace.created";
    public const string WorkspaceDeleted = "workspace.deleted";
    public const string WorkspaceMemberAdded = "workspace.member.added";
    public const string WorkspaceMemberRemoved = "workspace.member.removed";
    public const string WorkspaceMemberRoleChanged = "workspace.member.role_changed";

    // Project
    public const string ProjectCreated = "project.created";
    public const string ProjectDeleted = "project.deleted";
    public const string ProjectArchived = "project.archived";
    public const string ProjectUnarchived = "project.unarchived";
    public const string ProjectMemberAdded = "project.member.added";
    public const string ProjectMemberRemoved = "project.member.removed";
    public const string ProjectMemberRoleChanged = "project.member.role_changed";

    // Workflow (admin)
    public const string WorkflowCreated = "workflow.created";
    public const string WorkflowDeleted = "workflow.deleted";
}

public sealed class NoOpAuditLogger : IAuditLogger
{
    public Task LogAsync(string action, string scope, Guid? scopeId, object? payload, CancellationToken ct = default)
        => Task.CompletedTask;
}
