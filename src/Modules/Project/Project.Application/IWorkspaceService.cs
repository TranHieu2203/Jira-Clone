using BB.Common;

namespace Project.Application;

public interface IWorkspaceService
{
    Task<Result<WorkspaceDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkspaceDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkspaceDto>>> ListMineAsync(CancellationToken ct = default);
    Task<Result<WorkspaceDetailDto>> CreateAsync(CreateWorkspaceRequest request, CancellationToken ct = default);
    Task<Result<WorkspaceDetailDto>> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<Result<WorkspaceDetailDto>> AddMemberAsync(Guid id, AddWorkspaceMemberRequest request, CancellationToken ct = default);
    Task<Result<WorkspaceDetailDto>> RemoveMemberAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<WorkspaceDetailDto>> ChangeMemberRoleAsync(Guid id, Guid userId, ChangeWorkspaceMemberRoleRequest request, CancellationToken ct = default);
}
