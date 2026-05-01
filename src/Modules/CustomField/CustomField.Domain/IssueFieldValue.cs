using BB.Common;

namespace CustomField.Domain;

/// <summary>
/// EAV row: 1 row = 1 issue × 1 field. ValueJson chứa giá trị thực
/// (lưu qua IJsonColumn — Postgres jsonb / Oracle CLOB).
/// Indexed* columns được điền chỉ khi field.IsSearchable=true.
/// </summary>
public sealed class IssueFieldValue : BaseEntity, IEntityWithTrace
{
    public Guid IssueId { get; private set; }
    public Guid CustomFieldId { get; private set; }

    /// <summary>Schema chuẩn: { "v": &lt;value&gt; }.</summary>
    public string ValueJson { get; private set; } = "{}";

    public string? IndexedString { get; private set; }
    public decimal? IndexedNumber { get; private set; }
    public DateTimeOffset? IndexedDate { get; private set; }

    public string? CreatedTraceId { get; set; }

    private IssueFieldValue() { }

    public IssueFieldValue(Guid issueId, Guid customFieldId, string valueJson,
        string? indexedString = null, decimal? indexedNumber = null, DateTimeOffset? indexedDate = null)
    {
        IssueId = issueId;
        CustomFieldId = customFieldId;
        ValueJson = string.IsNullOrWhiteSpace(valueJson) ? "{}" : valueJson;
        IndexedString = indexedString;
        IndexedNumber = indexedNumber;
        IndexedDate = indexedDate;
    }

    public void UpdateValue(string valueJson, string? indexedString, decimal? indexedNumber, DateTimeOffset? indexedDate)
    {
        ValueJson = string.IsNullOrWhiteSpace(valueJson) ? "{}" : valueJson;
        IndexedString = indexedString;
        IndexedNumber = indexedNumber;
        IndexedDate = indexedDate;
    }
}
