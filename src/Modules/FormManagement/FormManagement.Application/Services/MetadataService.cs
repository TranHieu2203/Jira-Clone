using BB.Common;
using FluentValidation;
using FormManagement.Application.Repositories;
using FormManagement.Domain;
using Microsoft.Extensions.Logging;

namespace FormManagement.Application.Services;

public sealed class MetadataService : IMetadataService
{
    private readonly IMetadataRepository _repo;
    private readonly IFormManagementUnitOfWork _uow;
    private readonly IValidator<CreateMetadataRequest> _createValidator;
    private readonly IValidator<UpdateMetadataRequest> _updateValidator;
    private readonly ILogger<MetadataService> _logger;

    public MetadataService(
        IMetadataRepository repo,
        IFormManagementUnitOfWork uow,
        IValidator<CreateMetadataRequest> createValidator,
        IValidator<UpdateMetadataRequest> updateValidator,
        ILogger<MetadataService> logger)
    {
        _repo = repo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<MetadataDto>>> SearchAsync(string? keyword, string? group, CancellationToken ct = default)
    {
        var list = await _repo.SearchAsync(keyword, group, ct);
        return Result.Success<IReadOnlyList<MetadataDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<MetadataDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _repo.GetByIdAsync(id, ct);
        return m is null
            ? Result.Failure<MetadataDto>(ErrorType.NotFound, FormManagementErrors.MsgMetadataNotFound)
            : Result.Success(Mappers.ToDto(m));
    }

    public async Task<Result<MetadataDto>> CreateAsync(CreateMetadataRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var value = request.Value.Trim().ToUpperInvariant();
        if (await _repo.ValueExistsAsync(value, null, ct))
        {
            return Result.Failure<MetadataDto>(
                ErrorType.Conflict, FormManagementErrors.MsgMetadataValueDup,
                new[] { new ResultError(FormManagementErrors.MetadataValueDuplicated, FormManagementErrors.MsgMetadataValueDup, "value") });
        }

        var entity = new MetadataDef(value, request.Label, request.Type, request.Description, request.ValidationJson);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Metadata created Id={Id} Value={Value}", entity.Id, entity.Value);
        return Result.Success(Mappers.ToDto(entity), "form_mgmt.metadata.created.success", new { value = entity.Value });
    }

    public async Task<Result<MetadataDto>> UpdateAsync(Guid id, UpdateMetadataRequest request, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var m = await _repo.GetByIdAsync(id, ct);
        if (m is null)
            return Result.Failure<MetadataDto>(ErrorType.NotFound, FormManagementErrors.MsgMetadataNotFound);

        m.Update(request.Label, request.Type, request.Description, request.ValidationJson);
        _repo.Update(m);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(Mappers.ToDto(m), "form_mgmt.metadata.updated.success", new { value = m.Value });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _repo.GetByIdAsync(id, ct);
        if (m is null)
            return Result.Failure(ErrorType.NotFound, FormManagementErrors.MsgMetadataNotFound);

        // Soft delete — giữ history. Hard delete chỉ qua admin script.
        m.IsDeleted = true;
        m.DeletedAt = DateTimeOffset.UtcNow;
        _repo.Update(m);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(messageKey: "form_mgmt.metadata.deleted.success", messageArgs: new { value = m.Value });
    }
}
