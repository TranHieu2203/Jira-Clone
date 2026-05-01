namespace Project.Application;

// ============== Workspace ==============
public sealed record WorkspaceDto(
    Guid Id, string Name, string Slug, string? Description, string? AvatarUrl, Guid OwnerId,
    int MemberCount, DateTimeOffset CreatedAt);

public sealed record WorkspaceDetailDto(
    Guid Id, string Name, string Slug, string? Description, string? AvatarUrl, Guid OwnerId,
    IReadOnlyList<WorkspaceMemberDto> Members, DateTimeOffset CreatedAt);

public sealed record WorkspaceMemberDto(Guid UserId, int Role, DateTimeOffset JoinedAt);

public sealed record CreateWorkspaceRequest(string Name, string Slug, string? Description, string? AvatarUrl);
public sealed record UpdateWorkspaceRequest(string Name, string? Description, string? AvatarUrl);
public sealed record AddWorkspaceMemberRequest(Guid UserId, int Role);
public sealed record ChangeWorkspaceMemberRoleRequest(int Role);

// ============== Project ==============
public sealed record ProjectDto(
    Guid Id, Guid WorkspaceId, string Name, string Key, string? Description, string? AvatarUrl,
    Guid LeadId, int Type, bool IsArchived, int MemberCount, int IssueTypeCount,
    DateTimeOffset CreatedAt);

public sealed record ProjectDetailDto(
    Guid Id, Guid WorkspaceId, string Name, string Key, string? Description, string? AvatarUrl,
    Guid LeadId, int Type, bool IsArchived,
    IReadOnlyList<ProjectMemberDto> Members,
    IReadOnlyList<IssueTypeDto> IssueTypes,
    DateTimeOffset CreatedAt);

public sealed record ProjectMemberDto(Guid UserId, int Role, DateTimeOffset JoinedAt);
public sealed record IssueTypeDto(Guid Id, string Name, string Key, string? Icon, string? Color, int Order, bool IsSubtask, bool IsSystem);

public sealed record CreateProjectRequest(Guid WorkspaceId, string Name, string Key, Guid LeadId, int Type, string? Description);
public sealed record UpdateProjectRequest(string Name, string? Description, string? AvatarUrl);
public sealed record AddProjectMemberRequest(Guid UserId, int Role);
public sealed record ChangeProjectMemberRoleRequest(int Role);

public sealed record AddIssueTypeRequest(string Name, string Key, string? Icon, string? Color, bool IsSubtask);
public sealed record UpdateIssueTypeRequest(string Name, string? Icon, string? Color, int Order);
