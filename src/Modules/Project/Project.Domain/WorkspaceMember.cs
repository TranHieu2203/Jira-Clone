using BB.Common;

namespace Project.Domain;

public sealed class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private WorkspaceMember() { }

    internal WorkspaceMember(Guid workspaceId, Guid userId, WorkspaceRole role, DateTimeOffset joinedAt)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    internal void ChangeRole(WorkspaceRole role) => Role = role;
}
