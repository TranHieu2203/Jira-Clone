namespace Issue.Application.Repositories;

public sealed record IssueNotificationSnapshot(
    Guid IssueId,
    string IssueKey,
    Guid ProjectId,
    Guid? AssigneeId,
    IReadOnlyList<Guid> WatcherUserIds);

public interface IIssueNotificationSnapshotReader
{
    Task<IssueNotificationSnapshot?> GetAsync(Guid issueId, CancellationToken ct = default);
}
