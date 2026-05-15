using System.Text.Json;
using BB.Common;
using BB.Storage;
using FluentValidation;
using FormManagement.Application.Repositories;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;

namespace FormManagement.Application.Services;

public sealed class SubmissionService : ISubmissionService
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PdfContentType = "application/pdf";

    private readonly ISubmissionRepository _repo;
    private readonly ITemplateRepository _templateRepo;
    private readonly IFormManagementUnitOfWork _uow;
    private readonly IDocumentConversionService _conversion;
    private readonly IFileStorage _storage;
    private readonly IValidator<CreateSubmissionRequest> _createValidator;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository repo,
        ITemplateRepository templateRepo,
        IFormManagementUnitOfWork uow,
        IDocumentConversionService conversion,
        IFileStorage storage,
        IValidator<CreateSubmissionRequest> createValidator,
        ILogger<SubmissionService> logger)
    {
        _repo = repo;
        _templateRepo = templateRepo;
        _uow = uow;
        _conversion = conversion;
        _storage = storage;
        _createValidator = createValidator;
        _logger = logger;
    }

    /// <summary>
    /// Load template DOCX bytes: prefer S3 (StorageKey), fallback DB blob (legacy templates).
    /// Trả null nếu không có nguồn nào.
    /// </summary>
    private async Task<byte[]?> LoadTemplateBytesAsync(DocumentTemplate template, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(template.StorageKey))
        {
            await using var stream = await _storage.OpenReadAsync(template.StorageKey, ct);
            if (stream is null) return null;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        return template.DocxBytes;
    }

    public async Task<Result<IReadOnlyList<SubmissionDto>>> ListByTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        var list = await _repo.ListByTemplateAsync(templateId, take: 100, ct);
        return Result.Success<IReadOnlyList<SubmissionDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<SubmissionDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _repo.GetByIdAsync(id, ct);
        return s is null
            ? Result.Failure<SubmissionDto>(ErrorType.NotFound, FormManagementErrors.MsgSubmissionNotFound)
            : Result.Success(Mappers.ToDto(s));
    }

    public async Task<Result<SubmissionExportDto>> SubmitAndExportAsync(CreateSubmissionRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var template = await _templateRepo.GetByIdAsync(request.TemplateId, ct);
        if (template is null)
            return Result.Failure<SubmissionExportDto>(ErrorType.NotFound, FormManagementErrors.MsgTemplateNotFound);

        // Load template bytes — prefer S3, fallback DB blob.
        var templateBytes = await LoadTemplateBytesAsync(template, ct);
        if (templateBytes is null || templateBytes.Length == 0)
        {
            return Result.Failure<SubmissionExportDto>(
                ErrorType.Conflict, FormManagementErrors.MsgConversionUnsupported,
                new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) });
        }

        var dataJson = JsonSerializer.Serialize(request.Data);
        var submission = new Submission(template.Id, template.Version, dataJson, request.ExportFormat);
        await _repo.AddAsync(submission, ct);

        var mergeResult = await _conversion.MailMergeAsync(templateBytes, request.Data, request.ExportFormat, ct);
        if (mergeResult.IsFailure)
        {
            // Không persist submission khi merge fail — discard.
            _uow.DiscardChanges();
            return Result.Failure<SubmissionExportDto>(mergeResult);
        }

        var (fileName, contentType) = MapExportFormat(template.Code, request.ExportFormat);
        // Upload output bytes lên S3 → key dạng `submissions/{submissionId}/{filename}`.
        // OutputPath domain field giữ S3 key này. FE tải qua download endpoint sẽ stream từ S3.
        var outputKey = $"submissions/{submission.Id:N}/{fileName}";
        try
        {
            using var ms = new MemoryStream(mergeResult.Data!, writable: false);
            await _storage.PutAsync(outputKey, ms, contentType, ct);
            submission.AttachOutput(outputKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload submission output {SubmissionId} to S3 — continue, FE vẫn nhận bytes inline",
                submission.Id);
            // Không fail toàn bộ: FE đã có bytes trong response, BE chỉ mất audit trail.
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Submission created Id={Id} TemplateCode={Code} Format={Format} OutputKey={Key}",
            submission.Id, template.Code, request.ExportFormat, submission.OutputPath);

        return Result.Success(
            new SubmissionExportDto(Mappers.ToDto(submission), mergeResult.Data!, fileName, contentType),
            "form_mgmt.submission.submitted.success",
            new { code = template.Code });
    }

    private static (string FileName, string ContentType) MapExportFormat(string templateCode, ExportFormat format) =>
        format switch
        {
            ExportFormat.Docx => ($"{templateCode}.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            ExportFormat.Pdf => ($"{templateCode}.pdf", "application/pdf"),
            _ => ($"{templateCode}.bin", "application/octet-stream")
        };
}
