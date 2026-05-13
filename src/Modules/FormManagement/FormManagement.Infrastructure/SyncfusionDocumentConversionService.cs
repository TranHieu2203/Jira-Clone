using System.Globalization;
using BB.Common;
using FormManagement.Application;
using FormManagement.Application.Services;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;

namespace FormManagement.Infrastructure;

/// <summary>
/// Phase 7: hiện thực mail-merge bằng Syncfusion DocIO.
/// Import (Phase 6) vẫn delegate sang <see cref="OpenXmlDocumentConversionService"/> — lightweight + không
/// cần license; tránh phụ thuộc Syncfusion cho luồng chỉ-detect-placeholder.
///
/// Format vi-VN theo form.md §7:
///   prefix "I*"     → currency  "#,##0"   (Phí bảo hiểm, tổng tiền…)
///   suffix "*NGAY"  → ngày       "dd"     (BNGAY_CAP, JNGAY…)
///   suffix "*THANG" → tháng      "MM"
///   suffix "*NAM"   → năm        "yyyy"
/// </summary>
public sealed class SyncfusionDocumentConversionService : IDocumentConversionService
{
    private static readonly CultureInfo VnCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly OpenXmlDocumentConversionService _openXml;
    private readonly ILogger<SyncfusionDocumentConversionService> _logger;

    public SyncfusionDocumentConversionService(
        OpenXmlDocumentConversionService openXml,
        ILogger<SyncfusionDocumentConversionService> logger)
    {
        _openXml = openXml;
        _logger = logger;
    }

    /// <summary>Phase 6 delegate — OpenXml lo extract text + regex detect placeholder.</summary>
    public Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default) =>
        _openXml.ImportFromWordAsync(fileBytes, fileName, ct);

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
            using var inputStream = new MemoryStream(docxBytes);
            using var doc = new WordDocument(inputStream, DetectInputFormat(docxBytes));

            doc.MailMerge.MergeField += OnFieldMerging;

            var fieldNames = data.Keys.ToArray();
            var fieldValues = data.Values.Select(v => v?.ToString() ?? string.Empty).ToArray();
            doc.MailMerge.Execute(fieldNames, fieldValues);

            var outStream = new MemoryStream();
            doc.Save(outStream, MapOutputFormat(format));
            _logger.LogInformation("Mail-merge xong: {Count} field, output format {Format}, {Bytes} bytes",
                fieldNames.Length, format, outStream.Length);
            return Task.FromResult(Result.Success(outStream.ToArray(), "form_mgmt.export.success", new { fields = fieldNames.Length }));
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Mail-merge unsupported format");
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

    /// <summary>
    /// Magic bytes detection — DOCX bắt đầu PK\x03\x04 (zip); Word 2003 XML bắt đầu &lt;?xml.
    /// </summary>
    private static FormatType DetectInputFormat(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
            return FormatType.Docx;
        if (bytes.Length >= 5 && bytes[0] == 0x3C && bytes[1] == 0x3F && bytes[2] == 0x78 && bytes[3] == 0x6D && bytes[4] == 0x6C)
            return FormatType.WordML;
        return FormatType.Automatic;
    }

    private static FormatType MapOutputFormat(ExportFormat format) =>
        format switch
        {
            ExportFormat.WordML2003 => FormatType.WordML,
            ExportFormat.Docx => FormatType.Docx,
            // PDF cần Syncfusion.DocIORenderer.Net.Core (~40MB) — chưa add ở Phase 7. Throw để return 400.
            ExportFormat.Pdf => throw new NotSupportedException("PDF export requires Syncfusion.DocIORenderer (not added in Phase 7)."),
            _ => throw new NotSupportedException($"Unsupported ExportFormat: {format}")
        };

    /// <summary>
    /// FieldMergingEvents callback — format value theo prefix/suffix tên field (form.md §7).
    /// </summary>
    private static void OnFieldMerging(object sender, MergeFieldEventArgs args)
    {
        var name = args.FieldName ?? string.Empty;
        var raw = args.FieldValue?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            args.Text = string.Empty;
            return;
        }

        // I* → currency (giữ thứ tự check trước vì "I*" có thể match cả NGAY/THANG/NAM về mặt suffix).
        if (name.StartsWith("I", StringComparison.OrdinalIgnoreCase) &&
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            args.Text = amount.ToString("#,##0", VnCulture);
            return;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            if (name.EndsWith("NGAY", StringComparison.OrdinalIgnoreCase)) { args.Text = date.ToString("dd", VnCulture); return; }
            if (name.EndsWith("THANG", StringComparison.OrdinalIgnoreCase)) { args.Text = date.ToString("MM", VnCulture); return; }
            if (name.EndsWith("NAM", StringComparison.OrdinalIgnoreCase)) { args.Text = date.ToString("yyyy", VnCulture); return; }
        }

        // Default: raw as-is.
        args.Text = raw;
    }
}
