using BB.Common;
using BB.Security;
using Microsoft.Extensions.Logging;
using Project.Application.Repositories;
using Project.Domain;

namespace Project.Application;

public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _repo;
    private readonly IProjectUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<WorkspaceService> _logger;

    public WorkspaceService(IWorkspaceRepository repo, IProjectUnitOfWork uow, ICurrentUser currentUser, ILogger<WorkspaceService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<WorkspaceDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var w = await _repo.GetWithMembersAsync(id, ct);
        return w is null
            ? Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found")
            : Result.Success(Mappers.ToDetailDto(w));
    }

    public async Task<Result<WorkspaceDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var w = await _repo.GetBySlugAsync(slug, ct);
        return w is null
            ? Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found")
            : Result.Success(Mappers.ToDetailDto(w));
    }

    public async Task<Result<IReadOnlyList<WorkspaceDto>>> ListMineAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<WorkspaceDto>>(ErrorType.Unauthorized, "auth.required");

        var list = await _repo.ListByMemberAsync(_currentUser.UserId.Value, ct);
        return Result.Success<IReadOnlyList<WorkspaceDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<WorkspaceDetailDto>> CreateAsync(CreateWorkspaceRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<WorkspaceDetailDto>(ErrorType.Unauthorized, "auth.required");

        if (await _repo.SlugExistsAsync(request.Slug.ToLowerInvariant(), null, ct))
            return Result.Failure<WorkspaceDetailDto>(
                ErrorType.Conflict, ProjectErrors.MsgWsSlugDup,
                new[] { new ResultError(ProjectErrors.WsSlugDuplicated, ProjectErrors.MsgWsSlugDup, Field: "slug") });

        var w = new Workspace(request.Name, request.Slug, _currentUser.UserId.Value, request.Description, request.AvatarUrl);
        await _repo.AddAsync(w, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Workspace created Id={Id} Slug={Slug}", w.Id, w.Slug);
        return Result.Success(Mappers.ToDetailDto(w), "workspace.created.success", new { name = w.Name });
    }

    public async Task<Result<WorkspaceDetailDto>> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken ct = default)
    {
        var w = await _repo.GetWithMembersAsync(id, ct);
        if (w is null) return Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found");

        w.Rename(request.Name);
        w.UpdateDescription(request.Description);
        w.UpdateAvatar(request.AvatarUrl);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(Mappers.ToDetailDto(w), "workspace.updated.success");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(id, ct);
        if (w is null) return Result.Failure(ErrorType.NotFound, "workspace.not_found");
        _repo.Remove(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "workspace.deleted.success");
    }

    public async Task<Result<WorkspaceDetailDto>> AddMemberAsync(Guid id, AddWorkspaceMemberRequest request, CancellationToken ct = default)
    {
        Workspace? w = await _repo.GetWithMembersAsync(id, ct);
        if (w is null) return Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found");

        if (w.Members.Any(m => m.UserId == request.UserId))
        {
            return Result.Success(Mappers.ToDetailDto(w), "workspace.member.added");
        }

        WorkspaceRole role = (WorkspaceRole)request.Role;
        try
        {
            await _repo.AddWorkspaceMemberInsertOnlyAsync(w.Id, request.UserId, (int)role, ct);
        }
        catch (NotSupportedException)
        {
            w.AddMember(request.UserId, role);
            _repo.Update(w);
            await _uow.SaveChangesAsync(ct);
            return Result.Success(Mappers.ToDetailDto(w), "workspace.member.added");
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
        {
            // Unique (workspace_id, user_id) — idempotent under race.
            _logger.LogDebug(ex, "AddMember insert conflict Workspace={WorkspaceId} User={UserId}", w.Id, request.UserId);
        }

        Workspace? reloaded = await _repo.GetWithMembersAsync(id, ct);
        return reloaded is null
            ? Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found")
            : Result.Success(Mappers.ToDetailDto(reloaded), "workspace.member.added");
    }

    public async Task<Result<WorkspaceDetailDto>> RemoveMemberAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var w = await _repo.GetWithMembersAsync(id, ct);
        if (w is null) return Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found");

        w.RemoveMember(userId);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(w), "workspace.member.removed");
    }

    public async Task<Result<WorkspaceDetailDto>> ChangeMemberRoleAsync(Guid id, Guid userId, ChangeWorkspaceMemberRoleRequest request, CancellationToken ct = default)
    {
        var w = await _repo.GetWithMembersAsync(id, ct);
        if (w is null) return Result.Failure<WorkspaceDetailDto>(ErrorType.NotFound, "workspace.not_found");

        w.ChangeMemberRole(userId, (WorkspaceRole)request.Role);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(w), "workspace.member.role_changed");
    }
}
