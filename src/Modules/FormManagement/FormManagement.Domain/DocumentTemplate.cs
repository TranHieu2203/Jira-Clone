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
    /// <summary>Bản DOCX gốc (nếu import từ Word). Oracle-neutral byte[] → bytea Postgres / BLOB Oracle.</summary>
    public byte[]? DocxBytes { get; private set; }
    /// <summary>JSON array các metadata value được dùng. Vd: ["BSO_HD","CTEN","ITONG_PHI"].</summary>
    public string UsedFieldsJson { get; private set; } = "[]";
    public int Version { get; private set; } = 1;
    public TemplateStatus Status { get; private set; } = TemplateStatus.Draft;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private DocumentTemplate() { }

    public DocumentTemplate(string code, string name, string sfdtContent, string? category = null, byte[]? docxBytes = null, string? usedFieldsJson = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(FormManagementErrors.TemplateCodeRequired, FormManagementErrors.MsgTemplateCodeRequired);
        var trimmedCode = code.Trim().ToUpperInvariant();
        if (!CodePattern.IsMatch(trimmedCode))
            throw new DomainException(FormManagementErrors.TemplateCodeInvalid, FormManagementErrors.MsgTemplateCodeInvalid);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(FormManagementErrors.TemplateNameRequired, FormManagementErrors.MsgTemplateNameRequired);
        if (string.IsNullOrWhiteSpace(sfdtContent))
            throw new DomainException(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired);

        Code = trimmedCode;
        Name = name.Trim();
        Category = category?.Trim();
        SfdtContent = sfdtContent;
        DocxBytes = docxBytes;
        UsedFieldsJson = usedFieldsJson ?? "[]";
    }

    public void UpdateMetadata(string name, string? category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(FormManagementErrors.TemplateNameRequired, FormManagementErrors.MsgTemplateNameRequired);
        Name = name.Trim();
        Category = category?.Trim();
    }

    /// <summary>Update nội dung soạn thảo. Tăng version để FE biết submission cũ có thể đã lệch schema.</summary>
    public void UpdateContent(string sfdtContent, string usedFieldsJson)
    {
        if (string.IsNullOrWhiteSpace(sfdtContent))
            throw new DomainException(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired);
        SfdtContent = sfdtContent;
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
