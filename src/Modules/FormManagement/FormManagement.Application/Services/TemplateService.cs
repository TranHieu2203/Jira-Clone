using BB.Common;
using BB.Storage;
using FluentValidation;
using FormManagement.Application.Repositories;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;

namespace FormManagement.Application.Services;

public sealed class TemplateService : ITemplateService
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly ITemplateRepository _repo;
    private readonly IFormManagementUnitOfWork _uow;
    private readonly IDocumentConversionService _conversion;
    private readonly IFileStorage _storage;
    private readonly IValidator<CreateTemplateRequest> _createValidator;
    private readonly IValidator<UpdateTemplateMetadataRequest> _updateMetaValidator;
    private readonly IValidator<UpdateTemplateContentRequest> _updateContentValidator;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ITemplateRepository repo,
        IFormManagementUnitOfWork uow,
        IDocumentConversionService conversion,
        IFileStorage storage,
        IValidator<CreateTemplateRequest> createValidator,
        IValidator<UpdateTemplateMetadataRequest> updateMetaValidator,
        IValidator<UpdateTemplateContentRequest> updateContentValidator,
        ILogger<TemplateService> logger)
    {
        _repo = repo;
        _uow = uow;
        _conversion = conversion;
        _storage = storage;
        _createValidator = createValidator;
        _updateMetaValidator = updateMetaValidator;
        _updateContentValidator = updateContentValidator;
        _logger = logger;
    }

    /// <summary>
    /// Build S3 object key cho template DOCX. Format: <c>templates/{id}/v{version}.docx</c>.
    /// Mỗi version giữ object riêng → audit trail + rollback nếu cần.
    /// </summary>
    private static string BuildTemplateStorageKey(Guid templateId, int version) =>
        $"templates/{templateId:N}/v{version}.docx";

    /// <summary>
    /// Upload bytes lên S3. Trả về storage key đã upload. Throws nếu storage fail (caller handle).
    /// </summary>
    private async Task<string> UploadTemplateAsync(Guid templateId, int version, byte[] docxBytes, CancellationToken ct)
    {
        var key = BuildTemplateStorageKey(templateId, version);
        using var ms = new MemoryStream(docxBytes, writable: false);
        await _storage.PutAsync(key, ms, DocxContentType, ct);
        _logger.LogInformation("Template {Id} v{Version} uploaded to S3 key={Key} size={Bytes}b",
            templateId, version, key, docxBytes.Length);
        return key;
    }

    public async Task<Result<IReadOnlyList<TemplateSummaryDto>>> SearchAsync(string? keyword, TemplateStatus? status, string? category, CancellationToken ct = default)
    {
        var list = await _repo.SearchAsync(keyword, status, category, ct);
        return Result.Success<IReadOnlyList<TemplateSummaryDto>>(list.Select(Mappers.ToSummaryDto).ToList());
    }

    public async Task<Result<TemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        return t is null
            ? Result.Failure<TemplateDetailDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound)
            : Result.Success(Mappers.ToDetailDto(t));
    }

    public async Task<Result<TemplateDetailDto>> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var t = await _repo.GetByCodeAsync(code, ct);
        return t is null
            ? Result.Failure<TemplateDetailDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound)
            : Result.Success(Mappers.ToDetailDto(t));
    }

    public async Task<Result<TemplateDetailDto>> CreateAsync(CreateTemplateRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _repo.CodeExistsAsync(code, null, ct))
        {
            return Result.Failure<TemplateDetailDto>(
                ErrorType.Conflict, FormManagementErrors.MsgTemplateCodeDup,
                new[] { new ResultError(FormManagementErrors.TemplateCodeDuplicated, FormManagementErrors.MsgTemplateCodeDup, "code") });
        }

        var usedFieldsJson = Mappers.SerializeUsedFields(request.UsedFields ?? Array.Empty<string>());
        // DocxBase64 = DOCX gốc từ FE import. Decode + upload thẳng lên S3 thay vì giữ DB blob.
        byte[] docxBytes;
        try { docxBytes = Convert.FromBase64String(request.DocxBase64); }
        catch (FormatException)
        {
            return Result.Failure<TemplateDetailDto>(
                ErrorType.Validation, FormManagementErrors.MsgTemplateContentRequired,
                new[] { new ResultError(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired, "docxBase64") });
        }

        // Sinh Id trước → build storage key từ Id → upload → tạo entity với key only (không blob).
        var templateId = Guid.NewGuid();
        var storageKey = BuildTemplateStorageKey(templateId, version: 1);
        try
        {
            using var ms = new MemoryStream(docxBytes, writable: false);
            await _storage.PutAsync(storageKey, ms, DocxContentType, ct);
            _logger.LogInformation("Template {Id} v1 uploaded to S3 key={Key} size={Bytes}b",
                templateId, storageKey, docxBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload template DOCX to S3 — abort create");
            return Result.Failure<TemplateDetailDto>(
                ErrorType.Unexpected, FormManagementErrors.MsgConversionFailed);
        }

        var entity = new DocumentTemplate(code, request.Name, docxBytes: null, request.Category, usedFieldsJson, storageKey)
        {
            Id = templateId
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Template created Id={Id} Code={Code} StorageKey={Key}",
            entity.Id, entity.Code, entity.StorageKey);
        return Result.Success(Mappers.ToDetailDto(entity), "form_mgmt.template.created.success", new { code = entity.Code });
    }

    public async Task<Result<TemplateDetailDto>> UpdateMetadataAsync(Guid id, UpdateTemplateMetadataRequest request, CancellationToken ct = default)
    {
        await _updateMetaValidator.ValidateAndThrowAsync(request, ct);

        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure<TemplateDetailDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        t.UpdateMetadata(request.Name, request.Category);
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(t), "form_mgmt.template.updated.success", new { code = t.Code });
    }

    public async Task<Result<TemplateDetailDto>> UpdateContentAsync(Guid id, UpdateTemplateContentRequest request, CancellationToken ct = default)
    {
        await _updateContentValidator.ValidateAndThrowAsync(request, ct);

        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure<TemplateDetailDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        byte[] docxBytes;
        try { docxBytes = Convert.FromBase64String(request.DocxBase64); }
        catch (FormatException)
        {
            return Result.Failure<TemplateDetailDto>(
                ErrorType.Validation, FormManagementErrors.MsgTemplateContentRequired,
                new[] { new ResultError(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired, "docxBase64") });
        }
        // Upload version mới lên S3. Storage key = templates/{id}/v{newVersion}.docx (sau bump).
        var newStorageKey = await UploadTemplateAsync(t.Id, t.Version + 1, docxBytes, ct);
        t.UpdateContent(docxBytes: null, Mappers.SerializeUsedFields(request.UsedFields), newStorageKey);
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(t), "form_mgmt.template.content_saved.success", new { code = t.Code, version = t.Version });
    }

    public async Task<Result<TemplateDetailDto>> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure<TemplateDetailDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        t.Publish();
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(t), "form_mgmt.template.published.success", new { code = t.Code });
    }

    public async Task<Result<TemplateDetailDto>> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure<TemplateDetailDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        t.Archive();
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDetailDto(t), "form_mgmt.template.archived.success", new { code = t.Code });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        t.IsDeleted = true;
        t.DeletedAt = DateTimeOffset.UtcNow;
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "form_mgmt.template.deleted.success", messageArgs: new { code = t.Code });
    }

    public Task<Result<TemplateImportResultDto>> ImportFromWordAsync(byte[] fileBytes, string fileName, CancellationToken ct = default) =>
        _conversion.ImportFromWordAsync(fileBytes, fileName, ct);

    public async Task<Result<byte[]>> GetDocxBytesAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure<byte[]>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        // Prefer S3 (StorageKey set): stream qua IFileStorage, đọc full → byte array.
        // Fallback DB blob (legacy templates chưa migrate sang S3).
        if (!string.IsNullOrWhiteSpace(t.StorageKey))
        {
            await using var stream = await _storage.OpenReadAsync(t.StorageKey, ct);
            if (stream is null)
            {
                _logger.LogError("Template {Id} StorageKey={Key} not found in S3 — orphan entity?", t.Id, t.StorageKey);
                return Result.Failure<byte[]>(ErrorType.NotFound, FormManagementErrors.MsgTemplateContentRequired);
            }
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return Result.Success(ms.ToArray());
        }
        if (t.DocxBytes is { Length: > 0 })
            return Result.Success(t.DocxBytes);
        return Result.Failure<byte[]>(ErrorType.NotFound, FormManagementErrors.MsgTemplateContentRequired);
    }

    public async Task<Result> ReplaceDocxBytesAsync(Guid id, byte[] docxBytes, CancellationToken ct = default)
    {
        if (docxBytes is null || docxBytes.Length == 0)
            return Result.Failure(ErrorType.Validation, FormManagementErrors.MsgTemplateContentRequired);
        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        // Re-detect placeholders từ bytes mới: union MERGEFIELD instrText + plain text «NAME».
        var detected = _conversion.ExtractUsedFields(docxBytes);
        var usedFieldsJson = detected.Count > 0
            ? Mappers.SerializeUsedFields(detected)
            : t.UsedFieldsJson;

        // Upload version mới lên S3 → key mới (templates/{id}/v{newVersion}.docx).
        // Domain.UpdateContent với key set sẽ clear DocxBytes → DB không giữ blob nữa.
        var newStorageKey = await UploadTemplateAsync(t.Id, t.Version + 1, docxBytes, ct);
        t.UpdateContent(docxBytes: null, usedFieldsJson, newStorageKey);
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Template {Code} (Id={Id}) DOCX updated via OnlyOffice callback, version={V}, usedFields={Count}, S3Key={Key}",
            t.Code, t.Id, t.Version, detected.Count, newStorageKey);
        return Result.Success(messageKey: "form_mgmt.template.content_saved.success", messageArgs: new { code = t.Code, version = t.Version });
    }

    public async Task<Result> BackfillToS3Async(CancellationToken ct = default)
    {
        var all = await _repo.SearchAsync(null, null, null, ct);
        int processed = 0, skipped = 0, failed = 0;
        foreach (var t in all)
        {
            // Skip nếu đã có StorageKey hoặc không có DocxBytes để upload.
            if (!string.IsNullOrWhiteSpace(t.StorageKey))
            {
                skipped++;
                continue;
            }
            if (t.DocxBytes is null || t.DocxBytes.Length == 0)
            {
                skipped++;
                continue;
            }
            try
            {
                var key = BuildTemplateStorageKey(t.Id, t.Version);
                using var ms = new MemoryStream(t.DocxBytes, writable: false);
                await _storage.PutAsync(key, ms, DocxContentType, ct);
                // UpdateContent với key set → bump version + clear DocxBytes. Nhưng backfill
                // không nên bump version (vẫn cùng nội dung) → set qua reflection-free path:
                // gọi UpdateContent rồi sửa lại version. Đơn giản hơn: dùng EF set + repo Update.
                // Tránh thêm domain method backdoor → dùng raw EF qua repo.Update (sau set field).
                // Vì domain không expose setter, ta tạo entity mới với cùng Id + key, rồi swap.
                // Đơn giản hơn nữa: gọi UpdateContent(null, t.UsedFieldsJson, key) → version+1,
                // log warning. Acceptable cho backfill 1-time.
                t.UpdateContent(docxBytes: null, t.UsedFieldsJson, key);
                _repo.Update(t);
                processed++;
                _logger.LogInformation("Backfill template {Code} (Id={Id}) → S3 key={Key}, size={Bytes}b",
                    t.Code, t.Id, key, t.DocxBytes?.Length ?? 0);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Backfill template {Code} (Id={Id}) FAIL", t.Code, t.Id);
            }
        }
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Backfill done: processed={P} skipped={S} failed={F}", processed, skipped, failed);
        return Result.Success(
            messageKey: "form_mgmt.template.backfill.success",
            messageArgs: new { processed, skipped, failed });
    }
}
