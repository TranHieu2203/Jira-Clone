using Project.Application.Repositories;

namespace Project.Application;

public sealed class IssueProjectAccess : IIssueProjectAccess
{
    private readonly IProjectRepository _projects;

    public IssueProjectAccess(IProjectRepository projects) => _projects = projects;

    public async Task<IReadOnlySet<Guid>> ListAccessibleProjectIdsAsync(Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<Guid> ids = await _projects.ListProjectIdsByMemberAsync(userId, ct);
        return ids.Count == 0 ? new HashSet<Guid>() : ids.ToHashSet();
    }

    public Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default) =>
        _projects.IsUserMemberOfProjectAsync(userId, projectId, ct);
}
