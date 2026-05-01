using Project.Domain;

namespace Project.Application;

internal static class Mappers
{
    public static WorkspaceDto ToDto(Workspace w) =>
        new(w.Id, w.Name, w.Slug, w.Description, w.AvatarUrl, w.OwnerId, w.Members.Count, w.CreatedAt);

    public static WorkspaceDetailDto ToDetailDto(Workspace w) =>
        new(w.Id, w.Name, w.Slug, w.Description, w.AvatarUrl, w.OwnerId,
            w.Members.Select(ToDto).ToList(), w.CreatedAt);

    public static WorkspaceMemberDto ToDto(WorkspaceMember m) =>
        new(m.UserId, (int)m.Role, m.JoinedAt);

    public static ProjectDto ToDto(Domain.Project p) =>
        new(p.Id, p.WorkspaceId, p.Name, p.Key, p.Description, p.AvatarUrl, p.LeadId,
            (int)p.Type, p.IsArchived, p.Members.Count, p.IssueTypes.Count, p.CreatedAt);

    public static ProjectDetailDto ToDetailDto(Domain.Project p) =>
        new(p.Id, p.WorkspaceId, p.Name, p.Key, p.Description, p.AvatarUrl, p.LeadId,
            (int)p.Type, p.IsArchived,
            p.Members.Select(ToDto).ToList(),
            p.IssueTypes.OrderBy(t => t.Order).Select(ToDto).ToList(),
            p.CreatedAt);

    public static ProjectMemberDto ToDto(ProjectMember m) =>
        new(m.UserId, (int)m.Role, m.JoinedAt);

    public static IssueTypeDto ToDto(IssueType t) =>
        new(t.Id, t.Name, t.Key, t.Icon, t.Color, t.Order, t.IsSubtask, t.IsSystem);
}
