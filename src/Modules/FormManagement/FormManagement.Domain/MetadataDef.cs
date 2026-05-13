using System.Text.RegularExpressions;
using BB.Common;

namespace FormManagement.Domain;

/// <summary>
/// Định nghĩa một trường (metadata) dùng để chèn MERGEFIELD vào template biểu mẫu.
/// Convention value cho biểu mẫu bảo hiểm VN: prefix B/C/D/F/G/I/J/K/L/M.
/// </summary>
public sealed class MetadataDef : AggregateRoot, ISoftDeletable
{
    /// <summary>Quy tắc value: bắt đầu bằng chữ HOA, các ký tự tiếp theo là chữ HOA / số / underscore.</summary>
    public static readonly Regex ValuePattern = new("^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);

    public string Value { get; private set; } = string.Empty;     // BSO_HD
    public string Label { get; private set; } = string.Empty;     // "Số hợp đồng"
    public MetadataType Type { get; private set; }
    public string? FieldGroup { get; private set; }               // B|C|D|... — auto detect theo prefix khi tạo
    public string? Description { get; private set; }
    /// <summary>JSON serialized validation rules (min/max/pattern/required) — Oracle-neutral via TEXT/CLOB.</summary>
    public string? ValidationJson { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private MetadataDef() { }

    public MetadataDef(string value, string label, MetadataType type, string? description = null, string? validationJson = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(FormManagementErrors.MetadataValueRequired, FormManagementErrors.MsgMetadataValueRequired);
        if (!ValuePattern.IsMatch(value))
            throw new DomainException(FormManagementErrors.MetadataValueInvalid, FormManagementErrors.MsgMetadataValueInvalid);
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException(FormManagementErrors.MetadataLabelRequired, FormManagementErrors.MsgMetadataLabelRequired);

        Value = value.Trim().ToUpperInvariant();
        Label = label.Trim();
        Type = type;
        FieldGroup = DeriveGroup(Value);
        Description = description?.Trim();
        ValidationJson = validationJson;
    }

    public void Update(string label, MetadataType type, string? description, string? validationJson)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException(FormManagementErrors.MetadataLabelRequired, FormManagementErrors.MsgMetadataLabelRequired);

        Label = label.Trim();
        Type = type;
        Description = description?.Trim();
        ValidationJson = validationJson;
    }

    /// <summary>Group là ký tự đầu của value: BSO_HD → "B", ITONG_PHI → "I". Dùng để gom field ở sidebar / data-entry form.</summary>
    private static string? DeriveGroup(string value) =>
        string.IsNullOrEmpty(value) ? null : value[..1];
}
