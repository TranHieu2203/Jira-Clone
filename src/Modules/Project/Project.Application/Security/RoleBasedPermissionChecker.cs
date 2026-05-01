using BB.Security;
using Project.Application.Repositories;
using Project.Domain;

namespace Project.Application.Security;

/// <summary>
/// MVP impl: map cố định role → permission set (D6 - 4 role).
/// Phase ≥ P11 swap sang impl đọc permission scheme từ DB.
/// </summary>
public sealed class RoleBasedPermissionChecker : IPermissionChecker
{
    private readonly IProjectRepository _projectRepo;
    private readonly IWorkspaceRepository _workspaceRepo;

    public RoleBasedPermissionChecker(IProjectRepository projectRepo, IWorkspaceRepository workspaceRepo)
    {
        _projectRepo = projectRepo;
        _workspaceRepo = workspaceRepo;
    }

    public async Task<bool> HasOrgPermissionAsync(Guid userId, Guid orgId, string permission, CancellationToken ct = default)
    {
        var ws = await _workspaceRepo.GetWithMembersAsync(orgId, ct);
        if (ws is null) return false;
        var role = ws.RoleOf(userId);
        return role.HasValue && OrgPermissionsFor(role.Value).Contains(permission);
    }

    public async Task<bool> HasProjectPermissionAsync(Guid userId, Guid projectId, string permission, CancellationToken ct = default)
    {
        var p = await _projectRepo.GetWithDetailsAsync(projectId, ct);
        if (p is null) return false;
        var role = p.RoleOf(userId);
        if (role.HasValue && ProjectPermissionsFor(role.Value).Contains(permission)) return true;

        // Workspace owner/admin override mọi project trong workspace.
        var ws = await _workspaceRepo.GetWithMembersAsync(p.WorkspaceId, ct);
        var wsRole = ws?.RoleOf(userId);
        return wsRole is WorkspaceRole.Owner or WorkspaceRole.Admin;
    }

    public async Task<bool> IsInRoleAsync(Guid userId, Guid scopeId, string role, CancellationToken ct = default)
    {
        // scopeId thử cả 2 hướng — Workspace trước, Project sau.
        var ws = await _workspaceRepo.GetWithMembersAsync(scopeId, ct);
        if (ws is not null)
        {
            var r = ws.RoleOf(userId);
            return r.HasValue && string.Equals(MapWorkspaceRole(r.Value), role, StringComparison.OrdinalIgnoreCase);
        }
        var p = await _projectRepo.GetWithDetailsAsync(scopeId, ct);
        if (p is not null)
        {
            var r = p.RoleOf(userId);
            return r.HasValue && string.Equals(MapProjectRole(r.Value), role, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static IReadOnlyCollection<string> OrgPermissionsFor(WorkspaceRole role) => role switch
    {
        WorkspaceRole.Owner => new[] { PermissionKeys.OrgManage, PermissionKeys.OrgInviteMember },
        WorkspaceRole.Admin => new[] { PermissionKeys.OrgInviteMember },
        _ => Array.Empty<string>()
    };

    private static IReadOnlyCollection<string> ProjectPermissionsFor(ProjectRole role) => role switch
    {
        ProjectRole.Admin => new[]
        {
            PermissionKeys.ProjectView, PermissionKeys.ProjectManage,
            PermissionKeys.ProjectAdminWorkflow, PermissionKeys.ProjectAdminField,
            PermissionKeys.IssueCreate, PermissionKeys.IssueEdit, PermissionKeys.IssueDelete,
            PermissionKeys.IssueTransition, PermissionKeys.IssueAssign, PermissionKeys.IssueComment
        },
        ProjectRole.Member => new[]
        {
            PermissionKeys.ProjectView,
            PermissionKeys.IssueCreate, PermissionKeys.IssueEdit,
            PermissionKeys.IssueTransition, PermissionKeys.IssueAssign, PermissionKeys.IssueComment
        },
        ProjectRole.Viewer => new[]
        {
            PermissionKeys.ProjectView, PermissionKeys.IssueComment
        },
        _ => Array.Empty<string>()
    };

    private static string MapWorkspaceRole(WorkspaceRole r) => r switch
    {
        WorkspaceRole.Owner => Roles.OrgOwner,
        WorkspaceRole.Admin => "ORG_ADMIN",
        WorkspaceRole.Member => "ORG_MEMBER",
        _ => string.Empty
    };

    private static string MapProjectRole(ProjectRole r) => r switch
    {
        ProjectRole.Admin => Roles.ProjectAdmin,
        ProjectRole.Member => Roles.ProjectMember,
        ProjectRole.Viewer => Roles.ProjectViewer,
        _ => string.Empty
    };
}
