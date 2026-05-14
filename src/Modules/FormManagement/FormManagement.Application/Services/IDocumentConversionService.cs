using BB.Common;
using FormManagement.Domain;

namespace FormManagement.Application.Services;

/// <summary>
/// DOCX-only document operations: detect placeholders + mail-merge. Implementation dùng
/// DocumentFormat.OpenXml (Microsoft, MIT) + Clippit (OpenXmlPowerTools fork, MIT).
/// FE OnlyOffice DocServer render DOCX native → KHÔNG cần SFDT conversion ở BE.
/// </summary>
public interface IDocumentConversionService
{
    /// <summary>
    /// Phân tích DOCX, detect placeholder (regex form.md §2: /\.{3,}/, /_{3,}/, /\[[^\]]{1,50}\]/, /«([^»]+)»/).
    /// </summary>
    Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] docxBytes, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Mail-merge data vào template DOCX, output DOCX hoặc PDF.
    /// vi-VN formatting theo prefix/suffix tên field: I* → currency, *NGAY → dd, *THANG → MM, *NAM → yyyy.
    /// </summary>
    Task<Result<byte[]>> MailMergeAsync(byte[] docxBytes, IReadOnlyDictionary<string, object?> data, ExportFormat format, CancellationToken ct = default);
}
