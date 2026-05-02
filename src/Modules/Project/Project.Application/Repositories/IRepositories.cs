using BB.Persistence;
using Project.Domain;

namespace Project.Application.Repositories;

public interface IWorkspaceRepository : IRepository<Workspace>
{
    Task<Workspace?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
    Task<Workspace?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Workspace>> ListByMemberAsync(Guid userId, CancellationToken ct = default);
}

public interface IProjectRepository : IRepository<Domain.Project>
{
    Task<Domain.Project?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    /// <summary>Các project user là member có trùng key (khác workspace). Dùng để resolve GET by-key scoped member.</summary>
    Task<IReadOnlyList<Domain.Project>> ListWithDetailsByKeyForMemberAsync(Guid userId, string key, CancellationToken ct = default);
    Task<Domain.Project?> GetByKeyAsync(Guid workspaceId, string key, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(Guid workspaceId, string key, Guid? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Project>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Project>> ListByMemberAsync(Guid userId, CancellationToken ct = default);
    Task<IssueType?> GetIssueTypeByIdAsync(Guid issueTypeId, CancellationToken ct = default);
}

public interface IProjectUnitOfWork : IUnitOfWork { }
