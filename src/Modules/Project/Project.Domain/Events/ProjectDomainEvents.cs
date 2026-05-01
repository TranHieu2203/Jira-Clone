using BB.Common;

namespace Project.Domain.Events;

public sealed record WorkspaceCreated(Guid WorkspaceId, string Slug, Guid OwnerId) : DomainEvent;
public sealed record ProjectCreated(Guid ProjectId, Guid WorkspaceId, string Key) : DomainEvent;
public sealed record ProjectMemberAdded(Guid ProjectId, Guid UserId, ProjectRole Role) : DomainEvent;
