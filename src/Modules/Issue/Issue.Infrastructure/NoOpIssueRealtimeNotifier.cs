using Issue.Application;

namespace Issue.Infrastructure;

public sealed class NoOpIssueRealtimeNotifier : IIssueRealtimeNotifier
{
    public Task NotifyProjectBoardAsync(Guid projectId, IssueBoardRealtimeEvent payload, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task NotifyIssueThreadAsync(Guid issueId, IssueThreadRealtimeEvent payload, CancellationToken ct = default) =>
        Task.CompletedTask;
}
