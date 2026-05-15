using BB.Common;
using FluentValidation;
using FormManagement.Application.Repositories;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;

namespace FormManagement.Application.Services;

public sealed class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _repo;
    private readonly IFormManagementUnitOfWork _uow;
    private readonly IDocumentConversionService _conversion;
    private readonly IValidator<CreateTemplateRequest> _createValidator;
    private readonly IValidator<UpdateTemplateMetadataRequest> _updateMetaValidator;
    private readonly IValidator<UpdateTemplateContentRequest> _updateContentValidator;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ITemplateRepository repo,
        IFormManagementUnitOfWork uow,
        IDocumentConversionService conversion,
        IValidator<CreateTemplateRequest> createValidator,
        IValidator<UpdateTemplateMetadataRequest> updateMetaValidator,
        IValidator<UpdateTemplateContentRequest> updateContentValidator,
        ILogger<TemplateService> logger)
    {
        _repo = repo;
        _uow = uow;
        _conversion = conversion;
        _createValidator = createValidator;
        _updateMetaValidator = updateMetaValidator;
        _updateContentValidator = updateContentValidator;
        _logger = logger;
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
        // DocxBase64 = DOCX gốc từ FE import. Decode + persist vào DocumentTemplate.DocxBytes.
        // OnlyOffice DocServer sẽ fetch bytes này qua GET /templates/{id}/file để render.
        byte[] docxBytes;
        try { docxBytes = Convert.FromBase64String(request.DocxBase64); }
        catch (FormatException)
        {
            return Result.Failure<TemplateDetailDto>(
                ErrorType.Validation, FormManagementErrors.MsgTemplateContentRequired,
                new[] { new ResultError(FormManagementErrors.TemplateContentRequired, FormManagementErrors.MsgTemplateContentRequired, "docxBase64") });
        }
        var entity = new DocumentTemplate(code, request.Name, docxBytes, request.Category, usedFieldsJson);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Template created Id={Id} Code={Code}", entity.Id, entity.Code);
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
        t.UpdateContent(docxBytes, Mappers.SerializeUsedFields(request.UsedFields));
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
        if (t.DocxBytes is null || t.DocxBytes.Length == 0)
            return Result.Failure<byte[]>(ErrorType.NotFound, FormManagementErrors.MsgTemplateContentRequired);
        return Result.Success(t.DocxBytes);
    }

    public async Task<Result> ReplaceDocxBytesAsync(Guid id, byte[] docxBytes, CancellationToken ct = default)
    {
        if (docxBytes is null || docxBytes.Length == 0)
            return Result.Failure(ErrorType.Validation, FormManagementErrors.MsgTemplateContentRequired);
        var t = await _repo.GetByIdAsync(id, ct);
        if (t is null) return Result.Failure(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        // Re-detect placeholders từ bytes mới: union MERGEFIELD instrText + plain text «NAME».
        // Cần thiết vì user có thể add/remove field qua OnlyOffice editor — FE callback không gửi list.
        // Nếu detect rỗng (parse fail) → giữ list cũ để không mất data hiện hữu.
        var detected = _conversion.ExtractUsedFields(docxBytes);
        var usedFieldsJson = detected.Count > 0
            ? Mappers.SerializeUsedFields(detected)
            : t.UsedFieldsJson;

        t.UpdateContent(docxBytes, usedFieldsJson);
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Template {Code} (Id={Id}) DOCX updated via OnlyOffice callback, version={V}, usedFields={Count}",
            t.Code, t.Id, t.Version, detected.Count);
        return Result.Success(messageKey: "form_mgmt.template.content_saved.success", messageArgs: new { code = t.Code, version = t.Version });
    }
}
