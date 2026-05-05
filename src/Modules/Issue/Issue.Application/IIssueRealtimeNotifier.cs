namespace Issue.Application;

/// <summary>Đẩy sự kiện realtime tới FE (SignalR). Impl mặc định no-op; Api.Host thay bằng SignalR.</summary>
public interface IIssueRealtimeNotifier
{
    Task NotifyProjectBoardAsync(Guid projectId, IssueBoardRealtimeEvent payload, CancellationToken ct = default);

    Task NotifyIssueThreadAsync(Guid issueId, IssueThreadRealtimeEvent payload, CancellationToken ct = default);
}

public sealed record IssueBoardRealtimeEvent(string Action, Guid IssueId, string IssueKey);

public sealed record IssueThreadRealtimeEvent(string Action, Guid? CommentId = null);
