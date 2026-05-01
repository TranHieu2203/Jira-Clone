using BB.Common;

namespace Project.Application;

public interface IProjectService
{
    Task<Result<ProjectDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> GetByKeyAsync(Guid workspaceId, string key, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> ListMineAsync(CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default);
    Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<Result> UnarchiveAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<Result<ProjectDetailDto>> AddMemberAsync(Guid id, AddProjectMemberRequest request, CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> RemoveMemberAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> ChangeMemberRoleAsync(Guid id, Guid userId, ChangeProjectMemberRoleRequest request, CancellationToken ct = default);

    Task<Result<ProjectDetailDto>> AddIssueTypeAsync(Guid id, AddIssueTypeRequest request, CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> UpdateIssueTypeAsync(Guid id, Guid issueTypeId, UpdateIssueTypeRequest request, CancellationToken ct = default);
    Task<Result<ProjectDetailDto>> RemoveIssueTypeAsync(Guid id, Guid issueTypeId, CancellationToken ct = default);
}
