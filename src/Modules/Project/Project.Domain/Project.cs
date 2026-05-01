using System.Text.RegularExpressions;
using BB.Common;
using Project.Domain.Events;

namespace Project.Domain;

public sealed class Project : AggregateRoot, ISoftDeletable
{
    private static readonly Regex KeyPattern = new("^[A-Z][A-Z0-9]{1,9}$", RegexOptions.Compiled);

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;     // PRJ — unique trong 1 workspace
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Guid LeadId { get; private set; }
    public ProjectType Type { get; private set; }
    public bool IsArchived { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<ProjectMember> _members = new();
    public IReadOnlyList<ProjectMember> Members => _members;

    private readonly List<IssueType> _issueTypes = new();
    public IReadOnlyList<IssueType> IssueTypes => _issueTypes;

    /// <summary>Counter chạy sequential để sinh issue key (PRJ-1, PRJ-2…).</summary>
    public int NextIssueNumber { get; private set; } = 1;

    private Project() { }

    public Project(Guid workspaceId, string name, string key, Guid leadId, ProjectType type, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ProjectErrors.ProjectNameRequired, ProjectErrors.MsgProjectNameRequired);
        if (string.IsNullOrWhiteSpace(key) || !KeyPattern.IsMatch(key))
            throw new DomainException(ProjectErrors.ProjectKeyInvalid, ProjectErrors.MsgProjectKeyInvalid);

        WorkspaceId = workspaceId;
        Name = name.Trim();
        Key = key.Trim().ToUpperInvariant();
        Description = description;
        LeadId = leadId;
        Type = type;

        // Lead tự động là Admin
        _members.Add(new ProjectMember(Id, leadId, ProjectRole.Admin, DateTimeOffset.UtcNow));

        SeedDefaultIssueTypes();

        RaiseDomainEvent(new ProjectCreated(Id, WorkspaceId, Key));
    }

    private void SeedDefaultIssueTypes()
    {
        _issueTypes.Add(new IssueType(Id, "Epic", "EPIC", "epic", "#8B5CF6", 0, false, true));
        _issueTypes.Add(new IssueType(Id, "Story", "STORY", "story", "#10B981", 1, false, true));
        _issueTypes.Add(new IssueType(Id, "Task", "TASK", "task", "#3B82F6", 2, false, true));
        _issueTypes.Add(new IssueType(Id, "Bug", "BUG", "bug", "#EF4444", 3, false, true));
        _issueTypes.Add(new IssueType(Id, "Sub-task", "SUBTASK", "subtask", "#9CA3AF", 4, true, true));
    }

    public ProjectMember AddMember(Guid userId, ProjectRole role)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException(ProjectErrors.ProjectMemberDuplicated, ProjectErrors.MsgProjectMemberDup);
        var m = new ProjectMember(Id, userId, role, DateTimeOffset.UtcNow);
        _members.Add(m);
        return m;
    }

    public void RemoveMember(Guid userId)
    {
        var m = _members.FirstOrDefault(x => x.UserId == userId)
                ?? throw new DomainException(ProjectErrors.ProjectMemberNotFound, ProjectErrors.MsgProjectMemberNotFound);
        if (m.UserId == LeadId)
            throw new DomainException(ProjectErrors.ProjectCannotRemoveLead, ProjectErrors.MsgProjectCannotRemoveLead);
        _members.Remove(m);
    }

    public void ChangeMemberRole(Guid userId, ProjectRole role)
    {
        var m = _members.FirstOrDefault(x => x.UserId == userId)
                ?? throw new DomainException(ProjectErrors.ProjectMemberNotFound, ProjectErrors.MsgProjectMemberNotFound);
        m.ChangeRole(role);
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
    public ProjectRole? RoleOf(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    public IssueType AddIssueType(string name, string key, string? icon, string? color, bool isSubtask)
    {
        if (_issueTypes.Any(t => t.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException(ProjectErrors.IssueTypeKeyDuplicated, ProjectErrors.MsgIssueTypeKeyDup);
        var t = new IssueType(Id, name, key, icon, color, _issueTypes.Count, isSubtask, isSystem: false);
        _issueTypes.Add(t);
        return t;
    }

    public void RemoveIssueType(Guid issueTypeId)
    {
        var t = _issueTypes.FirstOrDefault(x => x.Id == issueTypeId)
                ?? throw new DomainException(ProjectErrors.IssueTypeNotFound, ProjectErrors.MsgIssueTypeNotFound);
        if (t.IsSystem)
            throw new DomainException(ProjectErrors.IssueTypeIsSystemCannotDelete, ProjectErrors.MsgIssueTypeSystem);
        _issueTypes.Remove(t);
    }

    public void UpdateIssueType(Guid issueTypeId, string name, string? icon, string? color, int order)
    {
        var t = _issueTypes.FirstOrDefault(x => x.Id == issueTypeId)
                ?? throw new DomainException(ProjectErrors.IssueTypeNotFound, ProjectErrors.MsgIssueTypeNotFound);
        t.Update(name, icon, color, order);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ProjectErrors.ProjectNameRequired, ProjectErrors.MsgProjectNameRequired);
        Name = name.Trim();
    }

    public void UpdateDescription(string? description) => Description = description;
    public void UpdateAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;
    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;
    public void TransferLead(Guid newLeadId)
    {
        if (!IsMember(newLeadId)) AddMember(newLeadId, ProjectRole.Admin);
        else ChangeMemberRole(newLeadId, ProjectRole.Admin);
        LeadId = newLeadId;
    }

    /// <summary>Lấy số issue tiếp theo (atomic — chỉ 1 caller được gọi trong 1 SaveChanges).</summary>
    public int AllocateIssueNumber()
    {
        var n = NextIssueNumber;
        NextIssueNumber++;
        return n;
    }
}
