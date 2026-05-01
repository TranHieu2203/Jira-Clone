using System.Text.RegularExpressions;
using BB.Common;

namespace Project.Domain;

public sealed class Workspace : AggregateRoot, ISoftDeletable
{
    private static readonly Regex SlugPattern = new("^[a-z][a-z0-9-]{1,49}$", RegexOptions.Compiled);

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;     // URL-friendly identifier
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Guid OwnerId { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<WorkspaceMember> _members = new();
    public IReadOnlyList<WorkspaceMember> Members => _members;

    private Workspace() { }

    public Workspace(string name, string slug, Guid ownerId, string? description = null, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ProjectErrors.WsNameRequired, ProjectErrors.MsgWsNameRequired);
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
            throw new DomainException(ProjectErrors.WsSlugInvalid, ProjectErrors.MsgWsSlugInvalid);

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        AvatarUrl = avatarUrl;
        OwnerId = ownerId;

        // Owner tự động trở thành thành viên với role Owner
        _members.Add(new WorkspaceMember(Id, ownerId, WorkspaceRole.Owner, DateTimeOffset.UtcNow));
    }

    public WorkspaceMember AddMember(Guid userId, WorkspaceRole role)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException(ProjectErrors.WsMemberDuplicated, ProjectErrors.MsgWsMemberDup);
        var m = new WorkspaceMember(Id, userId, role, DateTimeOffset.UtcNow);
        _members.Add(m);
        return m;
    }

    public void RemoveMember(Guid userId)
    {
        var m = _members.FirstOrDefault(x => x.UserId == userId)
                ?? throw new DomainException(ProjectErrors.WsMemberNotFound, ProjectErrors.MsgWsMemberNotFound);
        if (m.UserId == OwnerId)
            throw new DomainException(ProjectErrors.WsCannotRemoveOwner, ProjectErrors.MsgWsCannotRemoveOwner);
        _members.Remove(m);
    }

    public void ChangeMemberRole(Guid userId, WorkspaceRole role)
    {
        var m = _members.FirstOrDefault(x => x.UserId == userId)
                ?? throw new DomainException(ProjectErrors.WsMemberNotFound, ProjectErrors.MsgWsMemberNotFound);
        m.ChangeRole(role);
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
    public WorkspaceRole? RoleOf(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ProjectErrors.WsNameRequired, ProjectErrors.MsgWsNameRequired);
        Name = name.Trim();
    }

    public void UpdateDescription(string? description) => Description = description;
    public void UpdateAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;
    public void TransferOwnership(Guid newOwnerId)
    {
        if (!IsMember(newOwnerId)) AddMember(newOwnerId, WorkspaceRole.Owner);
        else ChangeMemberRole(newOwnerId, WorkspaceRole.Owner);
        ChangeMemberRole(OwnerId, WorkspaceRole.Admin);
        OwnerId = newOwnerId;
    }
}
