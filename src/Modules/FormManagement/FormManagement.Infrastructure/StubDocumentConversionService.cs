using BB.Common;
using FormManagement.Application;
using FormManagement.Application.Services;
using FormManagement.Domain;

namespace FormManagement.Infrastructure;

/// <summary>
/// Placeholder cho Phase 6/7. Phase 2 chưa cài Syncfusion.DocIO.Net.Core nên các flow yêu cầu
/// convert .docx ↔ SFDT hay mail-merge sẽ trả về Failure với message rõ ràng.
/// Khi vào Phase 6, swap class này bằng SyncfusionDocumentConversionService thật trong DI.
/// </summary>
public sealed class StubDocumentConversionService : IDocumentConversionService
{
    public Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default) =>
        Task.FromResult(Result.Failure<TemplateImportResultDto>(
            ErrorType.Conflict,
            FormManagementErrors.MsgConversionUnsupported,
            new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) }));

    public Task<Result<byte[]>> MailMergeAsync(byte[] docxBytes, IReadOnlyDictionary<string, object?> data, ExportFormat format, CancellationToken ct = default) =>
        Task.FromResult(Result.Failure<byte[]>(
            ErrorType.Conflict,
            FormManagementErrors.MsgConversionUnsupported,
            new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) }));
}
