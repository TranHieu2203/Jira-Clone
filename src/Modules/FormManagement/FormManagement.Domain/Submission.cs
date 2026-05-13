using BB.Common;

namespace FormManagement.Domain;

/// <summary>
/// Một lần user nhập data vào template và mail-merge ra file output. Bất biến sau khi submit.
/// </summary>
public sealed class Submission : AggregateRoot
{
    public Guid TemplateId { get; private set; }
    public int TemplateVersion { get; private set; }
    /// <summary>JSON serialized data { fieldValue: data }. Oracle-neutral CLOB/TEXT.</summary>
    public string DataJson { get; private set; } = "{}";
    public string? OutputPath { get; private set; }
    public ExportFormat ExportFormat { get; private set; } = ExportFormat.WordML2003;

    private Submission() { }

    public Submission(Guid templateId, int templateVersion, string dataJson, ExportFormat exportFormat = ExportFormat.WordML2003)
    {
        if (templateId == Guid.Empty)
            throw new DomainException(FormManagementErrors.TemplateNotFound, FormManagementErrors.MsgTemplateNotFound);
        if (string.IsNullOrWhiteSpace(dataJson) || dataJson == "{}")
            throw new DomainException(FormManagementErrors.SubmissionDataRequired, FormManagementErrors.MsgSubmissionDataRequired);

        TemplateId = templateId;
        TemplateVersion = templateVersion;
        DataJson = dataJson;
        ExportFormat = exportFormat;
    }

    public void AttachOutput(string outputPath) => OutputPath = outputPath;
}
