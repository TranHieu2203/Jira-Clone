using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BB.Common;
using Clippit;
using Clippit.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FormManagement.Application;
using FormManagement.Application.Services;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;

namespace FormManagement.Infrastructure;

/// <summary>
/// DOCX-only document service: detect placeholders + mail-merge.
/// <para>Stack: DocumentFormat.OpenXml (Microsoft, MIT) cho parsing + Clippit (OpenXmlPowerTools fork, MIT)
/// cho mail-merge field replacement. Không Syncfusion → không license, không trial watermark.</para>
/// <para>FE OnlyOffice DocumentEditor render DOCX native — BE không cần convert SFDT.</para>
/// </summary>
public sealed class OpenXmlDocumentConversionService : IDocumentConversionService
{
    private static readonly CultureInfo VnCulture = CultureInfo.GetCultureInfo("vi-VN");

    // 4 pattern theo form.md §2.
    private static readonly (string Pattern, Regex Regex)[] Patterns =
    {
        ("dots",        new Regex(@"\.{3,}",          RegexOptions.Compiled)),
        ("underscores", new Regex(@"_{3,}",           RegexOptions.Compiled)),
        ("brackets",    new Regex(@"\[[^\]]{1,50}\]", RegexOptions.Compiled)),
        ("guillemets",  new Regex(@"«([^»]+)»",       RegexOptions.Compiled))
    };

    private readonly ILogger<OpenXmlDocumentConversionService> _logger;

    public OpenXmlDocumentConversionService(ILogger<OpenXmlDocumentConversionService> logger)
    {
        _logger = logger;
    }

    public Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] docxBytes, string fileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".docx")
        {
            return Task.FromResult(Result.Failure<TemplateImportResultDto>(
                ErrorType.Validation, FormManagementErrors.MsgConversionUnsupported,
                new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported, "file") }));
        }

        try
        {
            var text = ExtractTextFromDocx(docxBytes);
            var placeholders = DetectPlaceholders(text);

            var dto = new TemplateImportResultDto(placeholders, Convert.ToBase64String(docxBytes));
            _logger.LogInformation("Imported {File}: {Count} placeholders, {Bytes} bytes",
                fileName, placeholders.Count, docxBytes.Length);
            return Task.FromResult(Result.Success(dto, "form_mgmt.import.success", new { count = placeholders.Count }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import {File}", fileName);
            return Task.FromResult(Result.Failure<TemplateImportResultDto>(
                ErrorType.Unexpected, FormManagementErrors.MsgConversionFailed,
                new[] { new ResultError(FormManagementErrors.ConversionFailed, FormManagementErrors.MsgConversionFailed) }));
        }
    }

    public Task<Result<byte[]>> MailMergeAsync(
        byte[] docxBytes,
        IReadOnlyDictionary<string, object?> data,
        ExportFormat format,
        CancellationToken ct = default)
    {
        if (docxBytes is null || docxBytes.Length == 0)
        {
            return Task.FromResult(Result.Failure<byte[]>(
                ErrorType.Validation, FormManagementErrors.MsgConversionUnsupported,
                new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) }));
        }

        try
        {
            // Pre-format data theo prefix/suffix tên field (vi-VN) trước khi merge.
            var formatted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in data)
            {
                formatted[kv.Key] = FormatFieldValue(kv.Key, kv.Value);
            }

            byte[] outputDocx = ReplaceMergeFieldsInDocx(docxBytes, formatted);

            if (format == ExportFormat.Docx)
            {
                _logger.LogInformation("Mail-merge done: {Fields} fields, {Bytes}b DOCX", formatted.Count, outputDocx.Length);
                return Task.FromResult(Result.Success(outputDocx, "form_mgmt.export.success", new { fields = formatted.Count }));
            }

            // PDF: cần OnlyOffice DocServer convert (Phase R7+). Hiện trả error rõ ràng.
            return Task.FromResult(Result.Failure<byte[]>(
                ErrorType.Validation, FormManagementErrors.MsgConversionUnsupported,
                new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mail-merge failed");
            return Task.FromResult(Result.Failure<byte[]>(
                ErrorType.Unexpected, FormManagementErrors.MsgConversionFailed,
                new[] { new ResultError(FormManagementErrors.ConversionFailed, FormManagementErrors.MsgConversionFailed) }));
        }
    }

    // ============== Extraction + detection ==============

    /// <summary>
    /// Đọc text từ DOCX bằng OpenXml SDK. Text trong nhiều &lt;w:t&gt; có thể bị tách bởi format
    /// runs → concatenate theo thứ tự để regex thấy chuỗi liên tục. Cũng đọc text trong header/footer.
    /// </summary>
    private static string ExtractTextFromDocx(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, isEditable: false);

        var sb = new System.Text.StringBuilder();
        // Body
        AppendParagraphsText(doc.MainDocumentPart?.Document?.Body, sb);
        // Headers + footers (watermark/page header có thể có placeholder).
        if (doc.MainDocumentPart is not null)
        {
            foreach (var hp in doc.MainDocumentPart.HeaderParts) AppendParagraphsText(hp.Header, sb);
            foreach (var fp in doc.MainDocumentPart.FooterParts) AppendParagraphsText(fp.Footer, sb);
        }
        return sb.ToString();
    }

    private static void AppendParagraphsText(DocumentFormat.OpenXml.OpenXmlElement? root, System.Text.StringBuilder sb)
    {
        if (root is null) return;
        foreach (var para in root.Descendants<Paragraph>())
        {
            foreach (var t in para.Descendants<Text>()) sb.Append(t.Text);
            sb.AppendLine();
        }
    }

    private static List<DetectedPlaceholderDto> DetectPlaceholders(string text)
    {
        var found = new List<DetectedPlaceholderDto>();
        foreach (var (name, regex) in Patterns)
        {
            foreach (Match match in regex.Matches(text))
            {
                if (!match.Success) continue;
                found.Add(new DetectedPlaceholderDto(match.Value, name, match.Index));
            }
        }
        return found.OrderBy(p => p.CharOffset).ToList();
    }

    // ============== Mail-merge ==============

    /// <summary>
    /// Replace MERGEFIELD trong DOCX. 2 strategy:
    /// <list type="number">
    /// <item>Walk fldChar/instrText structure (real MERGEFIELD từ DOCX template import).</item>
    /// <item>Regex replace plain text «VALUE» (field user insert qua OnlyOffice editor — chưa wrap fldChar).</item>
    /// </list>
    /// </summary>
    private static byte[] ReplaceMergeFieldsInDocx(byte[] docxBytes, IReadOnlyDictionary<string, string> dataFormatted)
    {
        using var ms = new MemoryStream();
        ms.Write(docxBytes, 0, docxBytes.Length);
        ms.Position = 0;

        using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
        {
            // Strategy 1: real MERGEFIELD walking.
            ReplaceInElement(doc.MainDocumentPart?.Document?.Body, dataFormatted);
            if (doc.MainDocumentPart is not null)
            {
                foreach (var hp in doc.MainDocumentPart.HeaderParts) ReplaceInElement(hp.Header, dataFormatted);
                foreach (var fp in doc.MainDocumentPart.FooterParts) ReplaceInElement(fp.Footer, dataFormatted);
            }
            // Strategy 2: plain text «VALUE» fallback (OnlyOffice insert).
            ReplaceGuillemetsText(doc.MainDocumentPart?.Document?.Body, dataFormatted);
            if (doc.MainDocumentPart is not null)
            {
                foreach (var hp in doc.MainDocumentPart.HeaderParts) ReplaceGuillemetsText(hp.Header, dataFormatted);
                foreach (var fp in doc.MainDocumentPart.FooterParts) ReplaceGuillemetsText(fp.Footer, dataFormatted);
            }
            doc.MainDocumentPart?.Document.Save();
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Replace plain text «VALUE» trong từng &lt;w:t&gt;. Lưu ý: text có thể bị tách runs do format,
    /// nên gom paragraph trước rồi replace bằng cách edit Text elements tuần tự.
    /// Simple version: per-paragraph, concatenate all w:t, regex replace, redistribute first Text + clear rest.
    /// </summary>
    private static void ReplaceGuillemetsText(DocumentFormat.OpenXml.OpenXmlElement? root, IReadOnlyDictionary<string, string> dataFormatted)
    {
        if (root is null) return;
        var fieldPattern = new Regex(@"«([^»]+)»", RegexOptions.Compiled);
        foreach (var para in root.Descendants<Paragraph>())
        {
            var texts = para.Descendants<Text>().ToList();
            if (texts.Count == 0) continue;
            var concat = string.Concat(texts.Select(t => t.Text));
            if (!concat.Contains('«')) continue;

            var replaced = fieldPattern.Replace(concat, match =>
            {
                var key = match.Groups[1].Value;
                return dataFormatted.TryGetValue(key, out var val) ? val : match.Value;
            });
            if (replaced == concat) continue;

            // Put entire replaced string into first Text; clear others.
            texts[0].Text = replaced;
            texts[0].Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
            for (int i = 1; i < texts.Count; i++) texts[i].Text = string.Empty;
        }
    }

    /// <summary>
    /// Walk OpenXml structure tìm MERGEFIELD complex field. Format:
    /// <code>
    /// &lt;w:fldChar w:fldCharType="begin"/&gt;
    /// &lt;w:instrText&gt; MERGEFIELD FieldName &lt;/w:instrText&gt;
    /// &lt;w:fldChar w:fldCharType="separate"/&gt;
    /// &lt;w:t&gt;«FieldName»&lt;/w:t&gt;            ← runs ở giữa được replace
    /// &lt;w:fldChar w:fldCharType="end"/&gt;
    /// </code>
    /// </summary>
    private static void ReplaceInElement(DocumentFormat.OpenXml.OpenXmlElement? root, IReadOnlyDictionary<string, string> dataFormatted)
    {
        if (root is null) return;

        // Regex extract field name từ instrText (vd " MERGEFIELD BSO_HD ").
        var fieldNameRegex = new Regex(@"\s*MERGEFIELD\s+(?<name>\S+)", RegexOptions.IgnoreCase);

        // Mỗi paragraph có thể có nhiều fields. Walk all runs để tìm fldChar begin/separate/end.
        var allRuns = root.Descendants<Run>().ToList();

        for (int i = 0; i < allRuns.Count; i++)
        {
            // Tìm fldChar begin
            var beginChar = allRuns[i].Descendants<FieldChar>().FirstOrDefault(f => f.FieldCharType?.Value == FieldCharValues.Begin);
            if (beginChar is null) continue;

            // Đọc instrText (có thể nằm trong run kế tiếp hoặc cùng run).
            string? fieldCode = null;
            int separateIdx = -1, endIdx = -1;
            for (int j = i; j < allRuns.Count; j++)
            {
                var instr = allRuns[j].Descendants<FieldCode>().FirstOrDefault();
                if (instr is not null && fieldCode is null) fieldCode = instr.Text;
                var fc = allRuns[j].Descendants<FieldChar>().FirstOrDefault();
                if (fc is null) continue;
                if (fc.FieldCharType?.Value == FieldCharValues.Separate) separateIdx = j;
                else if (fc.FieldCharType?.Value == FieldCharValues.End) { endIdx = j; break; }
            }
            if (fieldCode is null || separateIdx < 0 || endIdx < 0) continue;

            var match = fieldNameRegex.Match(fieldCode);
            if (!match.Success) continue;
            var fieldName = match.Groups["name"].Value;
            if (!dataFormatted.TryGetValue(fieldName, out var value)) continue;

            // Replace text trong runs giữa separate (exclusive) và end (exclusive).
            // Cách đơn giản: gom các Text giữa khoảng này thành 1, set value vào Text đầu tiên, xóa các Text khác.
            bool first = true;
            for (int j = separateIdx + 1; j < endIdx; j++)
            {
                foreach (var t in allRuns[j].Descendants<Text>().ToList())
                {
                    if (first) { t.Text = value; first = false; }
                    else t.Text = string.Empty;
                }
            }
        }
    }

    /// <summary>
    /// Format value theo prefix/suffix tên field (form.md §7). I* → currency, *NGAY/THANG/NAM → date parts.
    /// </summary>
    private static string FormatFieldValue(string fieldName, object? raw)
    {
        var s = raw?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(s)) return string.Empty;

        // Currency: prefix "I"
        if (fieldName.StartsWith("I", StringComparison.OrdinalIgnoreCase) &&
            decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount.ToString("#,##0", VnCulture);
        }

        // Date parts: suffix
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            if (fieldName.EndsWith("NGAY", StringComparison.OrdinalIgnoreCase)) return date.ToString("dd", VnCulture);
            if (fieldName.EndsWith("THANG", StringComparison.OrdinalIgnoreCase)) return date.ToString("MM", VnCulture);
            if (fieldName.EndsWith("NAM", StringComparison.OrdinalIgnoreCase)) return date.ToString("yyyy", VnCulture);
        }

        return s;
    }
}
