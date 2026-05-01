using BB.Common;

namespace Project.Domain;

public sealed class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private ProjectMember() { }

    internal ProjectMember(Guid projectId, Guid userId, ProjectRole role, DateTimeOffset joinedAt)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    internal void ChangeRole(ProjectRole role) => Role = role;
}
