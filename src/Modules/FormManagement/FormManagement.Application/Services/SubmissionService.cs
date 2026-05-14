using System.Text.Json;
using BB.Common;
using FluentValidation;
using FormManagement.Application.Repositories;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;

namespace FormManagement.Application.Services;

public sealed class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _repo;
    private readonly ITemplateRepository _templateRepo;
    private readonly IFormManagementUnitOfWork _uow;
    private readonly IDocumentConversionService _conversion;
    private readonly IValidator<CreateSubmissionRequest> _createValidator;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository repo,
        ITemplateRepository templateRepo,
        IFormManagementUnitOfWork uow,
        IDocumentConversionService conversion,
        IValidator<CreateSubmissionRequest> createValidator,
        ILogger<SubmissionService> logger)
    {
        _repo = repo;
        _templateRepo = templateRepo;
        _uow = uow;
        _conversion = conversion;
        _createValidator = createValidator;
        _logger = logger;
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

        if (template.DocxBytes is null || template.DocxBytes.Length == 0)
        {
            // Submission cần DOCX gốc để DocIO mail-merge. Template tạo từ SFDT only chưa có DOCX
            // → Phase 7 sẽ thêm bước "compile" SFDT → DOCX khi publish.
            return Result.Failure<SubmissionExportDto>(
                ErrorType.Conflict, FormManagementErrors.MsgConversionUnsupported,
                new[] { new ResultError(FormManagementErrors.ConversionUnsupported, FormManagementErrors.MsgConversionUnsupported) });
        }

        var dataJson = JsonSerializer.Serialize(request.Data);
        var submission = new Submission(template.Id, template.Version, dataJson, request.ExportFormat);
        await _repo.AddAsync(submission, ct);

        var mergeResult = await _conversion.MailMergeAsync(template.DocxBytes, request.Data, request.ExportFormat, ct);
        if (mergeResult.IsFailure)
        {
            // Không persist submission khi merge fail — discard.
            _uow.DiscardChanges();
            return Result.Failure<SubmissionExportDto>(mergeResult);
        }

        await _uow.SaveChangesAsync(ct);

        var (fileName, contentType) = MapExportFormat(template.Code, request.ExportFormat);
        _logger.LogInformation("Submission created Id={Id} TemplateCode={Code} Format={Format}",
            submission.Id, template.Code, request.ExportFormat);

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
