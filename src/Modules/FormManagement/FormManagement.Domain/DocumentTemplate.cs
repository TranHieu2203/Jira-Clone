using System.Text.RegularExpressions;
using BB.Common;

namespace FormManagement.Domain;

/// <summary>
/// Template biểu mẫu — chứa SFDT content (JSON của Syncfusion) và optional bản DOCX gốc (khi import từ Word).
/// UsedFields list các metadata value được dùng trong template — dùng để auto-generate data-entry form.
/// </summary>
public sealed class DocumentTemplate : AggregateRoot, ISoftDeletable
{
    /// <summary>Code template: chữ HOA / số / dash, 2-50 ký tự. Vd: "HD_BAO_HIEM_TS_2026".</summary>
    public static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9_-]{1,49}$", RegexOptions.Compiled);

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    /// <summary>SFDT JSON string — format native của Syncfusion DocumentEditor, load trực tiếp lên FE.</summary>
    public string SfdtContent { get; private set; } = string.Empty;
    /// <summary>
    /// Bản DOCX gốc (legacy storage trong DB blob). Sau khi migrate sang S3, field này null.
    /// Service layer dùng <c>StorageKey</c> trước; fallback đọc <c>DocxBytes</c> nếu key null
    /// (template cũ chưa migrate).
    /// </summary>
    public byte[]? DocxBytes { get; private set; }
    /// <summary>
    /// S3 object key. Format: <c>templates/{id}/v{version}.docx</c>. Service layer dùng key này
    /// để Put/Get qua <c>IFileStorage</c>. Null = template cũ chưa migrate (rare; backfill job).
    /// </summary>
    public string? StorageKey { get; private set; }
    /// <summary>JSON array các metadata value được dùng. Vd: ["BSO_HD","CTEN","ITONG_PHI"].</summary>
    public string UsedFieldsJson { get; private set; } = "[]";
    public int Version { get; private set; } = 1;
    public TemplateStatus Status { get; private set; } = TemplateStatus.Draft;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private DocumentTemplate() { }

    /// <summary>
    /// Tạo template. Phase OnlyOffice: <paramref name="docxBytes"/> là source of truth (FE editor
    /// fetch DOCX bytes qua endpoint, OnlyOffice DocServer render native). <c>SfdtContent</c> kept
    /// trong domain để giữ migration cũ — empty cho template tạo qua OnlyOffice flow.
    /// </summary>
    public DocumentTemplate(string code, string name, byte[]? docxBytes, string? category = null, string? usedFieldsJson = null, string? storageKey = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(FormManagementErrors.TemplateCodeRequired, FormManagementErrors.MsgTemplateCodeRequired);
        var trimmedCode = code.Trim().ToUpperInvariant();
        if (!CodePattern.IsMatch(trimmedCode))
            throw new DomainException(FormManagementErrors.TemplateCodeInvalid, FormManagementErrors.MsgTemplateCodeInvalid);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(FormManagementErrors.TemplateNameRequired, FormManagementErrors.MsgTemplateNameRequired);
        // Phải có ÍT NHẤT 1 trong 2: docxBytes (legacy DB blob) HOẶC storageKey (S3 path).
        bool hasBytes = docxBytes is { Length: > 0 };
        bool hasKey = !string.IsNullOrWhiteSpace(storageKey);
        if (!hasBytes && !hasKey)
            throw new DomainException(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired);

        Code = trimmedCode;
        Name = name.Trim();
        Category = category?.Trim();
        SfdtContent = string.Empty;
        DocxBytes = hasBytes ? docxBytes : null;
        StorageKey = hasKey ? storageKey : null;
        UsedFieldsJson = usedFieldsJson ?? "[]";
    }

    public void UpdateMetadata(string name, string? category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(FormManagementErrors.TemplateNameRequired, FormManagementErrors.MsgTemplateNameRequired);
        Name = name.Trim();
        Category = category?.Trim();
    }

    /// <summary>
    /// Replace DOCX content + bumped version (cho callback từ OnlyOffice DocServer).
    /// <paramref name="storageKey"/>: S3 key trỏ tới bytes vừa upload. Nếu null → giữ bytes
    /// trong <paramref name="docxBytes"/> như legacy.
    /// </summary>
    public void UpdateContent(byte[]? docxBytes, string usedFieldsJson, string? storageKey = null)
    {
        bool hasBytes = docxBytes is { Length: > 0 };
        bool hasKey = !string.IsNullOrWhiteSpace(storageKey);
        if (!hasBytes && !hasKey)
            throw new DomainException(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired);

        if (hasKey)
        {
            // S3 path: clear DB blob để tiết kiệm storage. Bytes giữ ở S3.
            StorageKey = storageKey;
            DocxBytes = null;
        }
        else
        {
            DocxBytes = docxBytes;
        }
        UsedFieldsJson = usedFieldsJson;
        Version++;
    }

    public void Publish()
    {
        if (Status == TemplateStatus.Archived)
            throw new DomainException(FormManagementErrors.TemplateStatusInvalid, FormManagementErrors.MsgTemplateStatusInvalid);
        Status = TemplateStatus.Published;
    }

    public void Archive() => Status = TemplateStatus.Archived;

    public void RevertToDraft()
    {
        if (Status == TemplateStatus.Archived)
            throw new DomainException(FormManagementErrors.TemplateStatusInvalid, FormManagementErrors.MsgTemplateStatusInvalid);
        Status = TemplateStatus.Draft;
    }
}
