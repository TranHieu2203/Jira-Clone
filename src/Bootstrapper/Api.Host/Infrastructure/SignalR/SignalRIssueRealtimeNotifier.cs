using Issue.Application;
using Microsoft.AspNetCore.SignalR;

namespace Api.Host.Infrastructure.SignalR;

public sealed class SignalRIssueRealtimeNotifier : IIssueRealtimeNotifier
{
    private readonly IHubContext<WorkspaceHub> _hub;
    private readonly ILogger<SignalRIssueRealtimeNotifier> _logger;

    public SignalRIssueRealtimeNotifier(IHubContext<WorkspaceHub> hub, ILogger<SignalRIssueRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyProjectBoardAsync(Guid projectId, IssueBoardRealtimeEvent payload, CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients.Group(HubGroups.Project(projectId)).SendAsync("BoardEvent", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR BoardEvent failed for project {ProjectId}", projectId);
        }
    }

    public async Task NotifyIssueThreadAsync(Guid issueId, IssueThreadRealtimeEvent payload, CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients.Group(HubGroups.Issue(issueId)).SendAsync("IssueEvent", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR IssueEvent failed for issue {IssueId}", issueId);
        }
    }
}
