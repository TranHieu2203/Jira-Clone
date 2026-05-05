using BB.Common;
using BB.Persistence;
using SprintEntity = global::Sprint.Domain.Sprint;
using Sprint.Domain;

namespace Sprint.Application.Repositories;

public interface ISprintRepository : IRepository<SprintEntity>
{
    Task<SprintEntity?> GetWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SprintEntity>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<SprintEntity?> GetActiveForProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<bool> HasOtherActiveSprintAsync(Guid projectId, Guid excludeSprintId, CancellationToken ct = default);

    /// <summary>Issue đang nằm trong sprint Planned hoặc Active của project.</summary>
    Task<IReadOnlySet<Guid>> GetIssueIdsInOpenSprintsAsync(Guid projectId, CancellationToken ct = default);

    Task<Guid?> FindOpenSprintIdForIssueAsync(Guid projectId, Guid issueId, Guid? excludeSprintId, CancellationToken ct = default);

    Task<IReadOnlyList<SprintCommitLine>> ListCommitLinesAsync(Guid sprintId, CancellationToken ct = default);
    Task AddCommitLinesAsync(IEnumerable<SprintCommitLine> lines, CancellationToken ct = default);
}

public interface ISprintUnitOfWork : IUnitOfWork;
