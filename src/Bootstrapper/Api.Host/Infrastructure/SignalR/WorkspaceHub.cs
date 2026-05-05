using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Host.Infrastructure.SignalR;

[Authorize]
public sealed class WorkspaceHub : Hub
{
    public Task JoinProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out Guid id))
            throw new HubException("invalid_project");

        return Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Project(id));
    }

    public Task LeaveProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out Guid id))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Project(id));
    }

    public Task JoinIssue(string issueId)
    {
        if (!Guid.TryParse(issueId, out Guid id))
            throw new HubException("invalid_issue");

        return Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Issue(id));
    }

    public Task LeaveIssue(string issueId)
    {
        if (!Guid.TryParse(issueId, out Guid id))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Issue(id));
    }
}

internal static class HubGroups
{
    internal static string Project(Guid projectId) => $"project:{projectId:N}";
    internal static string Issue(Guid issueId) => $"issue:{issueId:N}";
}
