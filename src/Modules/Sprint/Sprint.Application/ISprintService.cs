using BB.Common;

namespace Sprint.Application;

public interface ISprintService
{
    Task<Result<IReadOnlyList<SprintDto>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<SprintDto>> GetByIdAsync(Guid projectId, Guid sprintId, CancellationToken ct = default);
    Task<Result<SprintDto?>> GetActiveAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<SprintDto>> CreateAsync(Guid projectId, CreateSprintRequest request, CancellationToken ct = default);
    Task<Result<SprintDto>> UpdateAsync(Guid projectId, Guid sprintId, UpdateSprintRequest request, CancellationToken ct = default);
    Task<Result<SprintDto>> AddIssueAsync(Guid projectId, Guid sprintId, Guid issueId, CancellationToken ct = default);
    Task<Result> RemoveIssueAsync(Guid projectId, Guid sprintId, Guid issueId, CancellationToken ct = default);
    Task<Result<SprintDto>> ReorderIssuesAsync(Guid projectId, Guid sprintId, ReorderSprintIssuesRequest request, CancellationToken ct = default);
    Task<Result<SprintDto>> StartAsync(Guid projectId, Guid sprintId, CancellationToken ct = default);
    Task<Result<SprintDto>> CompleteAsync(Guid projectId, Guid sprintId, CancellationToken ct = default);
    Task<Result<SprintBurndownDto>> GetBurndownAsync(Guid projectId, Guid sprintId, CancellationToken ct = default);
}
