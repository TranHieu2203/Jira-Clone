using System.Text.RegularExpressions;
using BB.Common;

namespace Project.Domain;

public sealed class IssueType : BaseEntity
{
    private static readonly Regex KeyPattern = new("^[A-Z][A-Z0-9_]{1,29}$", RegexOptions.Compiled);

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Icon { get; private set; }       // tên icon hoặc URL nhỏ
    public string? Color { get; private set; }
    public int Order { get; private set; }
    public bool IsSubtask { get; private set; }
    public bool IsSystem { get; private set; }      // 5 type mặc định không cho xoá

    private IssueType() { }

    internal IssueType(Guid projectId, string name, string key, string? icon, string? color, int order, bool isSubtask, bool isSystem)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ProjectErrors.IssueTypeNameRequired, ProjectErrors.MsgIssueTypeNameRequired);
        if (!KeyPattern.IsMatch(key))
            throw new DomainException(ProjectErrors.IssueTypeKeyInvalid, ProjectErrors.MsgIssueTypeKeyInvalid);

        ProjectId = projectId;
        Name = name.Trim();
        Key = key.Trim().ToUpperInvariant();
        Icon = icon;
        Color = color;
        Order = order;
        IsSubtask = isSubtask;
        IsSystem = isSystem;
    }

    internal void Update(string name, string? icon, string? color, int order)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ProjectErrors.IssueTypeNameRequired, ProjectErrors.MsgIssueTypeNameRequired);
        Name = name.Trim();
        Icon = icon;
        Color = color;
        Order = order;
    }
}
