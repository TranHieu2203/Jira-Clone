namespace Project.Application;

/// <summary>
/// Cho phép module Issue giới hạn issue theo project mà user là thành viên (project_members).
/// </summary>
public interface IIssueProjectAccess
{
    Task<IReadOnlySet<Guid>> ListAccessibleProjectIdsAsync(Guid userId, CancellationToken ct = default);

    Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default);
}
