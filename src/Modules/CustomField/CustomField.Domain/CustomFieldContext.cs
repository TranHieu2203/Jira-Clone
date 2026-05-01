using BB.Common;

namespace CustomField.Domain;

/// <summary>
/// Phạm vi áp dụng của 1 field. Một field có thể có nhiều context (vd. global + project-cụ-thể).
/// Khi resolve, lookup theo (projectId, issueTypeId): ưu tiên context khớp cả 2, sau đó global.
/// </summary>
public sealed class CustomFieldContext : BaseEntity
{
    public Guid CustomFieldId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsGlobal { get; private set; }            // áp dụng mọi project
    public bool IsRequired { get; private set; }
    public string? DefaultValueJson { get; private set; }

    /// <summary>List project áp dụng (chỉ dùng khi !IsGlobal). Lưu dạng JSON array of guid.</summary>
    public List<Guid> ProjectIds { get; private set; } = new();

    /// <summary>List issueType áp dụng. Rỗng = mọi issueType.</summary>
    public List<Guid> IssueTypeIds { get; private set; } = new();

    private CustomFieldContext() { }

    internal CustomFieldContext(Guid customFieldId, string name, bool isGlobal, bool isRequired, string? defaultValueJson,
        IReadOnlyCollection<Guid>? projectIds = null,
        IReadOnlyCollection<Guid>? issueTypeIds = null)
    {
        CustomFieldId = customFieldId;
        Name = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        IsGlobal = isGlobal;
        IsRequired = isRequired;
        DefaultValueJson = defaultValueJson;
        if (projectIds is not null) ProjectIds.AddRange(projectIds);
        if (issueTypeIds is not null) IssueTypeIds.AddRange(issueTypeIds);
    }

    public bool AppliesTo(Guid projectId, Guid issueTypeId)
    {
        var projectMatch = IsGlobal || ProjectIds.Contains(projectId);
        var issueTypeMatch = IssueTypeIds.Count == 0 || IssueTypeIds.Contains(issueTypeId);
        return projectMatch && issueTypeMatch;
    }

    internal void Update(string name, bool isRequired, string? defaultValueJson)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        IsRequired = isRequired;
        DefaultValueJson = defaultValueJson;
    }

    internal void SetScope(bool isGlobal, IReadOnlyCollection<Guid> projectIds, IReadOnlyCollection<Guid> issueTypeIds)
    {
        IsGlobal = isGlobal;
        ProjectIds.Clear();
        ProjectIds.AddRange(projectIds);
        IssueTypeIds.Clear();
        IssueTypeIds.AddRange(issueTypeIds);
    }
}
