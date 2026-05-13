using BB.Common;
using FormManagement.Domain;

namespace FormManagement.Application.Services;

/// <summary>
/// Abstraction cho việc convert giữa Word formats và mail-merge với Syncfusion DocIO ở Infrastructure.
/// Implementation thật (Syncfusion DocIO) sẽ add ở Phase 6 (Import Word + Detect Placeholder)
/// và Phase 7 (Export Word 2003 XML). Hiện tại Infrastructure cung cấp stub trả về Failure.
/// </summary>
public interface IDocumentConversionService
{
    /// <summary>
    /// Convert .docx / .doc / Word 2003 XML → SFDT JSON. Đồng thời detect placeholder
    /// (regex: /\.{3,}/, /_{3,}/, /\[[^\]]{1,50}\]/, /«([^»]+)»/) cho FE highlight.
    /// </summary>
    Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Mail-merge dữ liệu vào template DOCX bytes, output file theo ExportFormat (WordML 2003 / DOCX / PDF).
    /// </summary>
    Task<Result<byte[]>> MailMergeAsync(byte[] docxBytes, IReadOnlyDictionary<string, object?> data, ExportFormat format, CancellationToken ct = default);
}
