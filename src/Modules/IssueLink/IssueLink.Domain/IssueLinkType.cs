namespace IssueLink.Domain;

/// <summary>
/// Loại quan hệ giữa hai issue. Mỗi cặp inverse được lưu thành 2 hằng số riêng để query
/// "outgoing/incoming theo issue X" đơn giản hơn (không cần resolve inverse runtime).
///
/// Ví dụ user click "A blocks B" → chỉ tạo 1 row <c>(source=A, target=B, type=Blocks)</c>.
/// FE khi xem issue B sẽ query incoming links có type=Blocks và hiển thị label "Blocked by A".
/// </summary>
public enum IssueLinkType
{
    /// <summary>Quan hệ chung — đối xứng (UI hiển thị "Relates to" cả 2 phía).</summary>
    RelatesTo = 1,

    /// <summary>Source chặn Target (Target không thể done trước Source).</summary>
    Blocks = 10,

    /// <summary>Source là duplicate của Target (UI thường gợi ý đóng Source và link tới Target).</summary>
    Duplicates = 20,

    /// <summary>Source được clone từ Target.</summary>
    Clones = 30,

    /// <summary>Source là cause của Target (bug → bug khác).</summary>
    Causes = 40
}

public static class IssueLinkTypeExtensions
{
    /// <summary>
    /// Inverse label cho FE — khi xem issue ở phía Target, label sẽ là gì.
    /// <list type="bullet">
    ///   <item>RelatesTo (đối xứng) → "Relates to"</item>
    ///   <item>Blocks → "Blocked by"</item>
    ///   <item>Duplicates → "Duplicated by"</item>
    ///   <item>Clones → "Cloned by"</item>
    ///   <item>Causes → "Caused by"</item>
    /// </list>
    /// </summary>
    public static string InverseKey(this IssueLinkType type) => type switch
    {
        IssueLinkType.RelatesTo => "relates_to",
        IssueLinkType.Blocks => "blocked_by",
        IssueLinkType.Duplicates => "duplicated_by",
        IssueLinkType.Clones => "cloned_by",
        IssueLinkType.Causes => "caused_by",
        _ => "related"
    };

    public static string ForwardKey(this IssueLinkType type) => type switch
    {
        IssueLinkType.RelatesTo => "relates_to",
        IssueLinkType.Blocks => "blocks",
        IssueLinkType.Duplicates => "duplicates",
        IssueLinkType.Clones => "clones",
        IssueLinkType.Causes => "causes",
        _ => "related"
    };
}
