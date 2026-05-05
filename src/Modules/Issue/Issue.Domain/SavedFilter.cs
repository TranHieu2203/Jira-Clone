using BB.Common;

namespace Issue.Domain;

/// <summary>
/// JQL filter user lưu lại cho search lặp đi lặp lại.
///
/// Ownership model (MVP):
/// - Owner = user tạo. Chỉ owner mới được sửa/xoá.
/// - <see cref="IsShared"/> = true → tất cả authenticated user xem được + apply được.
/// - Future P11+: scope theo project / org, ACL chi tiết.
/// </summary>
public sealed class SavedFilter : AggregateRoot
{
    public const int NameMaxLength = 120;
    /// <summary>2000 chars — sao cho khớp NVARCHAR2 max của Oracle 12c+ (đa-DB compat).</summary>
    public const int JqlMaxLength = 2000;
    public const int DescriptionMaxLength = 1000;

    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Jql { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsShared { get; private set; }

    private SavedFilter() { }

    public SavedFilter(Guid ownerUserId, string name, string jql, string? description, bool isShared)
    {
        EnsureName(name);
        EnsureJql(jql);
        EnsureDescription(description);

        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Jql = jql.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsShared = isShared;
    }

    public void Update(string name, string jql, string? description, bool isShared)
    {
        EnsureName(name);
        EnsureJql(jql);
        EnsureDescription(description);

        Name = name.Trim();
        Jql = jql.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsShared = isShared;
    }

    public void EnsureCanModify(Guid actorUserId)
    {
        if (actorUserId != OwnerUserId)
            throw new DomainException("SAVED_FILTER_NOT_OWNER", "saved_filter.not_owner");
    }

    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("SAVED_FILTER_NAME_REQUIRED", "saved_filter.name.required");
        if (name.Trim().Length > NameMaxLength)
            throw new DomainException("SAVED_FILTER_NAME_TOO_LONG", "saved_filter.name.too_long");
    }

    private static void EnsureJql(string jql)
    {
        if (string.IsNullOrWhiteSpace(jql))
            throw new DomainException("SAVED_FILTER_JQL_REQUIRED", "saved_filter.jql.required");
        if (jql.Length > JqlMaxLength)
            throw new DomainException("SAVED_FILTER_JQL_TOO_LONG", "saved_filter.jql.too_long");
    }

    private static void EnsureDescription(string? description)
    {
        if (description is { Length: > DescriptionMaxLength })
            throw new DomainException("SAVED_FILTER_DESCRIPTION_TOO_LONG", "saved_filter.description.too_long");
    }
}
