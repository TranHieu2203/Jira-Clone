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

    /// <summary>
    /// Post-process DOCX vừa save từ OnlyOffice callback: convert plain text «VALUE» → real OOXML
    /// MERGEFIELD (fldChar begin/instrText/separate/end structure). Lần sau editor mở doc, sẽ
    /// render với field shading + Alt+F9 toggle, đúng behavior MERGEFIELD chuẩn.
    /// Lý do cần: OnlyOffice plugin sandbox không tạo được real MERGEFIELD persist được —
    /// `range.AddField` log success nhưng serialize ra plain text. BE bù lại.
    /// </summary>
    byte[] WrapGuillemetsAsMergeFields(byte[] docxBytes);

    /// <summary>
    /// Detect distinct field codes used trong DOCX: union của
    ///   (1) MERGEFIELD instrText (real Word fields)
    ///   (2) plain text «NAME» (OnlyOffice plugin insert).
    /// Trả về list các code unique, giữ thứ tự xuất hiện. Dùng để rebuild
    /// UsedFields khi save qua OnlyOffice callback (FE không gửi list mới).
    /// </summary>
    IReadOnlyList<string> ExtractUsedFields(byte[] docxBytes);
}
