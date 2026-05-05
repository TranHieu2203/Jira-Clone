using BB.Common;
using BB.Security;
using Microsoft.Extensions.Logging;
using Project.Application.Repositories;
using Project.Domain;

namespace Project.Application;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _repo;
    private readonly IProjectUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionChecker _permissions;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository repo, IProjectUnitOfWork uow, ICurrentUser currentUser, IPermissionChecker permissions, IAuditLogger audit, ILogger<ProjectService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<ProjectDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        return p is null
            ? Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found")
            : Result.Success(Mappers.ToDetailDto(p));
    }

    public async Task<Result<ProjectDetailDto>> GetDetailForMemberByKeyAsync(string key, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<ProjectDetailDto>(ErrorType.Unauthorized, "auth.required");

        var matches = await _repo.ListWithDetailsByKeyForMemberAsync(_currentUser.UserId.Value, key, ct);
        if (matches.Count == 0)
            return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");
        if (matches.Count > 1)
            return Result.Failure<ProjectDetailDto>(
                ErrorType.Conflict, "project.key_ambiguous",
                new[] { new ResultError("PROJECT_KEY_AMBIGUOUS", "project.key_ambiguous", Field: "key") });

        return Result.Success(Mappers.ToDetailDto(matches[0]));
    }

    public async Task<Result<ProjectDetailDto>> GetByKeyAsync(Guid workspaceId, string key, CancellationToken ct = default)
    {
        var p = await _repo.GetByKeyAsync(workspaceId, key.ToUpperInvariant(), ct);
        return p is null
            ? Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found")
            : Result.Success(Mappers.ToDetailDto(p));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var list = await _repo.ListByWorkspaceAsync(workspaceId, ct);
        return Result.Success<IReadOnlyList<ProjectDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> ListMineAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<ProjectDto>>(ErrorType.Unauthorized, "auth.required");
        var list = await _repo.ListByMemberAsync(_currentUser.UserId.Value, ct);
        return Result.Success<IReadOnlyList<ProjectDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<ProjectDetailDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        // R2: tạo project = quản lý workspace. Owner + Admin được, Member không.
        var perm = await _permissions.RequireOrgAsync(_currentUser.UserId, request.WorkspaceId, PermissionKeys.OrgInviteMember, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        if (await _repo.KeyExistsAsync(request.WorkspaceId, request.Key.ToUpperInvariant(), null, ct))
            return Result.Failure<ProjectDetailDto>(
                ErrorType.Conflict, ProjectErrors.MsgProjectKeyDup,
                new[] { new ResultError(ProjectErrors.ProjectKeyDuplicated, ProjectErrors.MsgProjectKeyDup, Field: "key") });

        var p = new Domain.Project(request.WorkspaceId, request.Name, request.Key, request.LeadId, (ProjectType)request.Type, request.Description);
        await _repo.AddAsync(p, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Project created Id={Id} Key={Key}", p.Id, p.Key);
        await _audit.LogAsync(AuditActions.ProjectCreated, "project", p.Id, new { p.Key, p.Name, p.WorkspaceId }, ct);
        return Result.Success(Mappers.ToDetailDto(p), "project.created.success", new { key = p.Key });
    }

    public async Task<Result<ProjectDetailDto>> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        // R2: chỉ Project Admin được cập nhật metadata project.
        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.Rename(request.Name);
        p.UpdateDescription(request.Description);
        p.UpdateAvatar(request.AvatarUrl);
        _repo.Update(p);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(p), "project.updated.success");
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p is null) return Result.Failure(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return perm;

        p.Archive(); _repo.Update(p); await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.ProjectArchived, "project", p.Id, new { p.Key }, ct);
        return Result.Success(messageKey: "project.archived");
    }

    public async Task<Result> UnarchiveAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p is null) return Result.Failure(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return perm;

        p.Unarchive(); _repo.Update(p); await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.ProjectUnarchived, "project", p.Id, new { p.Key }, ct);
        return Result.Success(messageKey: "project.unarchived");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p is null) return Result.Failure(ErrorType.NotFound, "project.not_found");

        // R2: delete project — workspace Owner/Admin (workspace override) hoặc Project Admin.
        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return perm;

        _repo.Remove(p); await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.ProjectDeleted, "project", p.Id, new { p.Key, p.Name }, ct);
        return Result.Success(messageKey: "project.deleted");
    }

    public async Task<Result<ProjectDetailDto>> AddMemberAsync(Guid id, AddProjectMemberRequest request, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.AddMember(request.UserId, (ProjectRole)request.Role);
        _repo.Update(p); await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.ProjectMemberAdded, "project", p.Id, new { request.UserId, role = request.Role }, ct);
        return Result.Success(Mappers.ToDetailDto(p), "project.member.added");
    }

    public async Task<Result<ProjectDetailDto>> RemoveMemberAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.RemoveMember(userId);
        _repo.Update(p); await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.ProjectMemberRemoved, "project", p.Id, new { userId }, ct);
        return Result.Success(Mappers.ToDetailDto(p), "project.member.removed");
    }

    public async Task<Result<ProjectDetailDto>> ChangeMemberRoleAsync(Guid id, Guid userId, ChangeProjectMemberRoleRequest request, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectManage, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.ChangeMemberRole(userId, (ProjectRole)request.Role);
        _repo.Update(p); await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.ProjectMemberRoleChanged, "project", p.Id, new { userId, role = request.Role }, ct);
        return Result.Success(Mappers.ToDetailDto(p), "project.member.role_changed");
    }

    public async Task<Result<ProjectDetailDto>> AddIssueTypeAsync(Guid id, AddIssueTypeRequest request, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        // R2: quản lý issue type = field admin.
        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectAdminField, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.AddIssueType(request.Name, request.Key, request.Icon, request.Color, request.IsSubtask);
        _repo.Update(p); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(p), "issue_type.added");
    }

    public async Task<Result<ProjectDetailDto>> UpdateIssueTypeAsync(Guid id, Guid issueTypeId, UpdateIssueTypeRequest request, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectAdminField, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.UpdateIssueType(issueTypeId, request.Name, request.Icon, request.Color, request.Order);
        _repo.Update(p); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(p), "issue_type.updated");
    }

    public async Task<Result<ProjectDetailDto>> RemoveIssueTypeAsync(Guid id, Guid issueTypeId, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(id, ct);
        if (p is null) return Result.Failure<ProjectDetailDto>(ErrorType.NotFound, "project.not_found");

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, p.Id, PermissionKeys.ProjectAdminField, ct);
        if (perm.IsFailure) return Result.Failure<ProjectDetailDto>(perm);

        p.RemoveIssueType(issueTypeId);
        _repo.Update(p); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(p), "issue_type.removed");
    }
}
