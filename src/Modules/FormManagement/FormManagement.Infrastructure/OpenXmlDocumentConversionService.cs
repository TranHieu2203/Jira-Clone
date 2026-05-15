using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BB.Common;
using Clippit;
using Clippit.Word;
using DocumentFormat.OpenXml;
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
        // Strict identifier pattern (cùng lý do với ExtractUsedFields): tránh greedy match qua
        // nested « khi OnlyOffice split display run của MERGEFIELD và chèn plain «NEW» vào giữa,
        // tạo chuỗi như "«CCHUC_VU«NEW»" — greedy regex sẽ bắt nguyên cụm, fail dict lookup, miss NEW.
        var fieldPattern = new Regex(@"«([A-Za-z]\w*)»", RegexOptions.Compiled);
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
    /// Post-process DOCX vừa save từ OnlyOffice: scan plain text «VALUE» trong document.xml →
    /// wrap thành real OOXML MERGEFIELD (fldChar begin/instrText/separate/end).
    /// Lần sau OnlyOffice mở doc, sẽ render field với gray shading + Alt+F9 toggle.
    /// </summary>
    /// <summary>
    /// Detect distinct field codes trong DOCX. Strategy:
    ///  - MERGEFIELD: walk &lt;w:instrText&gt; tìm pattern "MERGEFIELD NAME".
    ///  - Plain «NAME»: regex trên concatenated text body+headers+footers.
    /// Filter ra code hợp lệ ([A-Za-z][A-Za-z0-9_]*) để bỏ noise.
    /// Trả về unique theo thứ tự xuất hiện (Word + plain).
    /// </summary>
    public IReadOnlyList<string> ExtractUsedFields(byte[] docxBytes)
    {
        if (docxBytes is null || docxBytes.Length == 0) return Array.Empty<string>();
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codeRegex = new Regex(@"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
        var mergeFieldNameRegex = new Regex(@"\s*MERGEFIELD\s+(?<name>\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Strict identifier match: dùng `[A-Za-z]\w*` thay vì `[^»]+` để không bị greedy
        // bắt qua các « lồng nhau khi OnlyOffice split display run của MERGEFIELD và chèn plain
        // «CEMAIL» vào giữa — ví dụ concat "«CCHUC_VU«CEMAIL»" với greedy thì match cả "CCHUC_VU«CEMAIL"
        // rồi fail identifier filter → mất CEMAIL.
        var guillemetRegex = new Regex(@"«([A-Za-z]\w*)»", RegexOptions.Compiled);

        void Add(string raw)
        {
            var name = raw.Trim().TrimEnd('\\').Trim();
            if (string.IsNullOrEmpty(name)) return;
            if (!codeRegex.IsMatch(name)) return;
            if (seen.Add(name)) ordered.Add(name);
        }

        try
        {
            using var ms = new MemoryStream();
            ms.Write(docxBytes, 0, docxBytes.Length);
            ms.Position = 0;
            using var doc = WordprocessingDocument.Open(ms, isEditable: false);

            void ScanInstr(OpenXmlElement? root)
            {
                if (root is null) return;
                foreach (var instr in root.Descendants<FieldCode>())
                {
                    var m = mergeFieldNameRegex.Match(instr.Text ?? string.Empty);
                    if (m.Success) Add(m.Groups["name"].Value);
                }
            }
            void ScanGuillemets(OpenXmlElement? root)
            {
                if (root is null) return;
                var sb = new System.Text.StringBuilder();
                foreach (var para in root.Descendants<Paragraph>())
                {
                    foreach (var t in para.Descendants<Text>()) sb.Append(t.Text);
                    sb.AppendLine();
                }
                foreach (Match m in guillemetRegex.Matches(sb.ToString()))
                {
                    Add(m.Groups[1].Value);
                }
            }

            ScanInstr(doc.MainDocumentPart?.Document?.Body);
            ScanGuillemets(doc.MainDocumentPart?.Document?.Body);
            if (doc.MainDocumentPart is not null)
            {
                foreach (var hp in doc.MainDocumentPart.HeaderParts) { ScanInstr(hp.Header); ScanGuillemets(hp.Header); }
                foreach (var fp in doc.MainDocumentPart.FooterParts) { ScanInstr(fp.Footer); ScanGuillemets(fp.Footer); }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ExtractUsedFields failed — return partial {Count}", ordered.Count);
        }
        return ordered;
    }

    public byte[] WrapGuillemetsAsMergeFields(byte[] docxBytes)
    {
        if (docxBytes is null || docxBytes.Length == 0) return docxBytes ?? Array.Empty<byte>();
        var pattern = new Regex(@"«([A-Za-z][A-Za-z0-9_]*)»", RegexOptions.Compiled);

        using var ms = new MemoryStream();
        ms.Write(docxBytes, 0, docxBytes.Length);
        ms.Position = 0;
        try
        {
            using (var doc = WordprocessingDocument.Open(ms, true))
            {
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body is null) return docxBytes;

                void WrapRoot(OpenXmlElement? root)
                {
                    if (root is null) return;
                    foreach (var para in root.Descendants<Paragraph>().ToList())
                    {
                        WrapInParagraph(para, pattern);
                    }
                }

                WrapRoot(body);
                if (doc.MainDocumentPart is not null)
                {
                    foreach (var hp in doc.MainDocumentPart.HeaderParts) { WrapRoot(hp.Header); hp.Header.Save(); }
                    foreach (var fp in doc.MainDocumentPart.FooterParts) { WrapRoot(fp.Footer); fp.Footer.Save(); }
                }
                doc.MainDocumentPart!.Document.Save();
            }
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WrapGuillemetsAsMergeFields fail — return original bytes");
            return docxBytes;
        }
    }

    /// <summary>
    /// Per-RUN wrap với fldChar depth tracking. Chiến lược:
    ///  1. Walk runs theo doc order, track depth từ fldChar begin/end.
    ///  2. Mỗi Run được phân loại "starts-in-field" (depth>0 khi bắt đầu Run) hoặc free.
    ///  3. Chỉ wrap «NAME» trong các Run free (depth=0) VÀ không chứa fldChar/FieldCode.
    ///     → KHÔNG động chạm display run của MERGEFIELD hiện hữu (depth>0).
    ///     → KHÔNG re-wrap fldChar scaffolding runs (begin/instrText/separate/end).
    ///     → CÓ wrap «NEW» plain mới chèn vào paragraph dù paragraph đã có MERGEFIELD khác.
    ///
    /// Trade-off: «...» bị split qua nhiều Run (vd OnlyOffice tách "«X" và "»" ra 2 Run) sẽ
    /// không match per-Run → giữ plain. Mail-merge regex Strategy 2 vẫn substitute đúng.
    /// </summary>
    private static void WrapInParagraph(Paragraph para, Regex pattern)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count == 0) return;

        // Bước 1: pre-compute "starts-in-field" cho mỗi Run dựa trên fldChar order.
        var startsInField = new bool[runs.Count];
        int depth = 0;
        for (int i = 0; i < runs.Count; i++)
        {
            startsInField[i] = depth > 0;
            foreach (var fc in runs[i].Descendants<FieldChar>())
            {
                var type = fc.FieldCharType?.Value;
                if (type == FieldCharValues.Begin) depth++;
                else if (type == FieldCharValues.End) depth = Math.Max(0, depth - 1);
                // Separate giữ nguyên depth.
            }
        }

        // Bước 2: với mỗi Run free + không chứa fldChar/FieldCode + text có «NAME» → wrap inline.
        // Dùng snapshot list runs (không enum trực tiếp para.Elements để tránh modify-while-iterate).
        for (int i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            if (run.Parent is null) continue; // đã bị remove ở vòng trước (không xảy ra với logic này)
            if (startsInField[i]) continue;
            bool hasFieldStuff = run.Descendants<FieldChar>().Any() || run.Descendants<FieldCode>().Any();
            if (hasFieldStuff) continue;

            var texts = run.Descendants<Text>().ToList();
            if (texts.Count == 0) continue;
            var concat = string.Concat(texts.Select(t => t.Text));
            if (!concat.Contains('«')) continue;
            var matches = pattern.Matches(concat);
            if (matches.Count == 0) continue;

            var rPrTemplate = run.RunProperties?.CloneNode(true) as RunProperties;
            var newElements = new List<OpenXmlElement>();
            int cursor = 0;
            foreach (Match m in matches)
            {
                if (m.Index > cursor)
                    newElements.Add(BuildPlainRun(concat.Substring(cursor, m.Index - cursor), rPrTemplate));
                newElements.AddRange(BuildMergeFieldRuns(m.Groups[1].Value, rPrTemplate));
                cursor = m.Index + m.Length;
            }
            if (cursor < concat.Length)
                newElements.Add(BuildPlainRun(concat.Substring(cursor), rPrTemplate));

            // Insert sau Run hiện tại theo thứ tự, rồi remove Run cũ.
            OpenXmlElement prev = run;
            foreach (var el in newElements)
            {
                run.Parent!.InsertAfter(el, prev);
                prev = el;
            }
            run.Remove();
        }
    }

    private static Run BuildPlainRun(string text, RunProperties? rPrTemplate)
    {
        var run = new Run();
        if (rPrTemplate is not null) run.AppendChild(rPrTemplate.CloneNode(true));
        run.AppendChild(new Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
        return run;
    }

    /// <summary>
    /// Tạo sequence 3 Run cho MERGEFIELD complex field:
    ///  1. Run với fldChar begin + instrText "MERGEFIELD NAME \* MERGEFORMAT"
    ///  2. Run với fldChar separate + Text "«NAME»" (display value)
    ///  3. Run với fldChar end
    /// </summary>
    private static IEnumerable<Run> BuildMergeFieldRuns(string fieldName, RunProperties? rPrTemplate)
    {
        // Run 1: begin + instrText
        var run1 = new Run();
        if (rPrTemplate is not null) run1.AppendChild(rPrTemplate.CloneNode(true));
        run1.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
        var instr = new FieldCode($" MERGEFIELD {fieldName} \\* MERGEFORMAT ")
        {
            Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve
        };
        run1.AppendChild(instr);
        // Có version đẩy separate vào run riêng — để tương thích, tách run.
        yield return run1;

        // Run 2: separate + display text «NAME»
        var run2 = new Run();
        if (rPrTemplate is not null) run2.AppendChild(rPrTemplate.CloneNode(true));
        run2.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Separate });
        yield return run2;

        var run3 = new Run();
        if (rPrTemplate is not null) run3.AppendChild(rPrTemplate.CloneNode(true));
        run3.AppendChild(new Text($"«{fieldName}»") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
        yield return run3;

        // Run 4: end
        var run4 = new Run();
        if (rPrTemplate is not null) run4.AppendChild(rPrTemplate.CloneNode(true));
        run4.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
        yield return run4;
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
