using Issue.Application.Repositories;
using Project.Application;

namespace Issue.Application;

/// <summary>
/// Mặc định: tra ProjectId của issue qua repository, sau đó delegate tới
/// <see cref="IIssueProjectAccess"/> để check membership.
/// </summary>
public sealed class IssueAccessGuard : IIssueAccessGuard
{
    private readonly IIssueRepository _issues;
    private readonly IIssueProjectAccess _projectAccess;

    public IssueAccessGuard(IIssueRepository issues, IIssueProjectAccess projectAccess)
    {
        _issues = issues;
        _projectAccess = projectAccess;
    }

    public async Task<IssueAccessSnapshot?> ResolveAccessAsync(Guid userId, Guid issueId, CancellationToken ct = default)
    {
        // Lấy issue (chỉ cần ProjectId — dùng GetByIdAsync để khỏi load watchers).
        Domain.Issue? issue = await _issues.GetByIdAsync(issueId, ct);
        if (issue is null)
            return null;

        // Membership check — không leak existence khi user không có quyền.
        if (!await _projectAccess.CanAccessProjectAsync(userId, issue.ProjectId, ct))
            return null;

        return new IssueAccessSnapshot(issue.Id, issue.ProjectId);
    }
}
