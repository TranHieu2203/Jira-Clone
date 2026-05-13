using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BB.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FormManagement.Application;
using FormManagement.Application.Services;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EjWord = Syncfusion.EJ2.DocumentEditor;

namespace FormManagement.Infrastructure;

/// <summary>
/// Hiện thực <see cref="IDocumentConversionService"/> bằng DocumentFormat.OpenXml (MIT, Microsoft official).
/// Phase 6 chỉ cần detect placeholder — SFDT JSON sẽ do FE Syncfusion DocumentEditor tự load qua
/// <c>documentEditor.open(file)</c> client-side.
///
/// Mail-merge (Phase 7) vẫn chưa hỗ trợ vì cần Syncfusion DocIO/DocumentEditor server-side (license-locked).
/// </summary>
public sealed class OpenXmlDocumentConversionService : IDocumentConversionService
{
    // 4 pattern theo form.md §2 + value extract regex riêng cho «...» để lưu tên field.
    private static readonly (string Pattern, Regex Regex)[] Patterns =
    {
        ("dots",        new Regex(@"\.{3,}",         RegexOptions.Compiled)),
        ("underscores", new Regex(@"_{3,}",          RegexOptions.Compiled)),
        ("brackets",    new Regex(@"\[[^\]]{1,50}\]", RegexOptions.Compiled)),
        ("guillemets",  new Regex(@"«([^»]+)»",      RegexOptions.Compiled))
    };

    private readonly ILogger<OpenXmlDocumentConversionService> _logger;

    public OpenXmlDocumentConversionService(ILogger<OpenXmlDocumentConversionService> logger)
    {
        _logger = logger;
    }

    public Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        try
        {
            string text = ext switch
            {
                ".docx" => ExtractTextFromDocx(fileBytes),
                ".xml"  => ExtractTextFromWord2003Xml(fileBytes),
                _ => throw new NotSupportedException($"Unsupported file format: {ext}")
            };

            var placeholders = DetectPlaceholders(text);
            // Convert DOCX/Word2003XML → SFDT JSON dùng Syncfusion.EJ2.WordEditor.AspNet.Core.
            // FE Syncfusion DocumentEditor v33 không có client-side DOCX converter (cần serviceUrl HOẶC SFDT input);
            // ta serialize SFDT ở BE → trả qua API → FE gọi documentEditor.open(sfdt).
            var sfdt = ConvertToSfdt(fileBytes, ext);

            var dto = new TemplateImportResultDto(sfdt, placeholders);
            _logger.LogInformation("Imported {File}: {Count} placeholders detected, SFDT {Bytes} bytes",
                fileName, placeholders.Count, sfdt.Length);
            return Task.FromResult(Result.Success(dto, "form_mgmt.import.success", new { count = placeholders.Count }));
        }
        catch (NotSupportedException)
        {
            return Task.FromResult(Result.Failure<TemplateImportResultDto>(
                ErrorType.Validation, FormManagementErrors.MsgConversionUnsupported,
                new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported, "file") }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import {File}", fileName);
            return Task.FromResult(Result.Failure<TemplateImportResultDto>(
                ErrorType.Unexpected, FormManagementErrors.MsgConversionFailed,
                new[] { new ResultError(FormManagementErrors.ConversionFailed, FormManagementErrors.MsgConversionFailed) }));
        }
    }

    public Task<Result<byte[]>> MailMergeAsync(byte[] docxBytes, IReadOnlyDictionary<string, object?> data, ExportFormat format, CancellationToken ct = default)
    {
        // Phase 7 sẽ implement bằng Syncfusion.DocIO.Net.Core. Hiện trả ConversionUnsupported.
        return Task.FromResult(Result.Failure<byte[]>(
            ErrorType.Conflict,
            FormManagementErrors.MsgConversionUnsupported,
            new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) }));
    }

    /// <summary>
    /// Convert DOCX/DOC/Word 2003 XML/RTF → SFDT JSON dùng Syncfusion.EJ2.WordEditor.AspNet.Core.
    /// FE editor sẽ gọi <c>documentEditor.open(sfdtString)</c>.
    /// </summary>
    private static string ConvertToSfdt(byte[] bytes, string ext)
    {
        using var stream = new MemoryStream(bytes);
        var format = ext switch
        {
            ".docx" => EjWord.FormatType.Docx,
            ".doc"  => EjWord.FormatType.Doc,
            ".xml"  => EjWord.FormatType.WordML,
            ".rtf"  => EjWord.FormatType.Rtf,
            ".txt"  => EjWord.FormatType.Txt,
            _ => EjWord.FormatType.Docx
        };
        var document = EjWord.WordDocument.Load(stream, format);
        var sfdt = JsonConvert.SerializeObject(document);
        document.Dispose();
        // Scrub track-changes / revisions flag từ SFDT trước khi gửi xuống FE:
        // Syncfusion editor restore enableTrackChanges từ documentSettings ngay sau open() →
        // override property set ở client. Cách an toàn nhất: bỏ flag ngay trong JSON gốc.
        return ScrubTrackChanges(sfdt);
    }

    /// <summary>
    /// Patch SFDT JSON: set protectionType=NoProtection, clear revisions array, đặt
    /// trackChanges/enforcement=false. Idempotent — không thêm field nếu chưa tồn tại.
    /// </summary>
    private static string ScrubTrackChanges(string sfdt)
    {
        try
        {
            var obj = Newtonsoft.Json.Linq.JObject.Parse(sfdt);
            // Common SFDT v33 settings field names. Set false nếu tồn tại.
            foreach (var key in new[] { "trackChanges", "enforcement", "isLockChanges" })
            {
                if (obj[key] != null) obj[key] = false;
            }
            // Clear revisions collection (existing tracked-changes từ Word gốc).
            if (obj["revisions"] is Newtonsoft.Json.Linq.JArray)
            {
                obj["revisions"] = new Newtonsoft.Json.Linq.JArray();
            }
            return obj.ToString(Newtonsoft.Json.Formatting.None);
        }
        catch
        {
            // Nếu parse fail vì SFDT shape khác kỳ vọng → trả SFDT gốc, không block import.
            return sfdt;
        }
    }

    /// <summary>
    /// Đọc text từ .docx bằng OpenXml SDK. DOCX là ZIP package chứa word/document.xml;
    /// text trong nhiều phần tử &lt;w:t&gt; có thể bị tách bởi format runs — ta concatenate
    /// theo thứ tự document để regex thấy chuỗi liên tục.
    /// </summary>
    private static string ExtractTextFromDocx(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var para in body.Descendants<Paragraph>())
        {
            foreach (var t in para.Descendants<Text>())
            {
                sb.Append(t.Text);
            }
            // Newline giữa paragraph để regex không vượt biên đoạn (vd dấu "..." cuối câu).
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Đọc text từ Word 2003 XML (file XML thuần, namespace w="http://schemas.microsoft.com/office/word/2003/wordml").
    /// </summary>
    private static string ExtractTextFromWord2003Xml(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var xdoc = XDocument.Load(ms);
        XNamespace w = "http://schemas.microsoft.com/office/word/2003/wordml";

        var sb = new System.Text.StringBuilder();
        foreach (var para in xdoc.Descendants(w + "p"))
        {
            foreach (var t in para.Descendants(w + "t"))
            {
                sb.Append(t.Value);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Chạy 4 regex pattern → trả list placeholder (text gốc + pattern name + offset trong text concat).
    /// </summary>
    private static List<DetectedPlaceholderDto> DetectPlaceholders(string text)
    {
        var found = new List<DetectedPlaceholderDto>();
        foreach (var (name, regex) in Patterns)
        {
            foreach (Match match in regex.Matches(text))
            {
                if (!match.Success) continue;
                found.Add(new DetectedPlaceholderDto(
                    Text: match.Value,
                    Pattern: name,
                    CharOffset: match.Index));
            }
        }
        // Sort theo vị trí xuất hiện để FE highlight tuần tự — UX panel "Đã phát hiện ...".
        return found.OrderBy(p => p.CharOffset).ToList();
    }
}
