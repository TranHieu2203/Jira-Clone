using Issue.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Issue.Infrastructure;

public sealed class IssueNotificationSnapshotReader : IIssueNotificationSnapshotReader
{
    private readonly IssueDbContext _ctx;

    public IssueNotificationSnapshotReader(IssueDbContext ctx) => _ctx = ctx;

    public async Task<IssueNotificationSnapshot?> GetAsync(Guid issueId, CancellationToken ct = default)
    {
        Domain.Issue? issue = await _ctx.Issues.AsNoTracking()
            .Include(i => i.Watchers)
            .FirstOrDefaultAsync(i => i.Id == issueId, ct);

        if (issue is null)
            return null;

        List<Guid> watchers = issue.Watchers.Select(w => w.UserId).Distinct().ToList();
        return new IssueNotificationSnapshot(issue.Id, issue.Key, issue.ProjectId, issue.AssigneeId, watchers);
    }
}
