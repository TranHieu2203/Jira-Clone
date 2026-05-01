using BB.Common;
using CustomField.Application.Repositories;
using CustomField.Domain;
using Microsoft.Extensions.Logging;

namespace CustomField.Application;

public sealed class CustomFieldService : ICustomFieldService
{
    private readonly ICustomFieldRepository _repo;
    private readonly ICustomFieldUnitOfWork _uow;
    private readonly ILogger<CustomFieldService> _logger;

    public CustomFieldService(ICustomFieldRepository repo, ICustomFieldUnitOfWork uow, ILogger<CustomFieldService> logger)
    {
        _repo = repo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<CustomFieldDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        return f is null
            ? Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found")
            : Result.Success(Mappers.ToDto(f));
    }

    public async Task<Result<CustomFieldDto>> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        var f = await _repo.GetByKeyAsync(key.ToLowerInvariant(), ct);
        return f is null
            ? Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found")
            : Result.Success(Mappers.ToDto(f));
    }

    public async Task<Result<IReadOnlyList<CustomFieldDto>>> ListAsync(CancellationToken ct = default)
    {
        var list = await _repo.ListAllAsync(ct);
        return Result.Success<IReadOnlyList<CustomFieldDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<CustomFieldDto>>> ResolveForAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default)
    {
        var list = await _repo.ResolveForAsync(projectId, issueTypeId, ct);
        return Result.Success<IReadOnlyList<CustomFieldDto>>(list.Select(Mappers.ToDto).ToList());
    }

    public async Task<Result<CustomFieldDto>> CreateAsync(CreateCustomFieldRequest request, CancellationToken ct = default)
    {
        if (await _repo.KeyExistsAsync(request.Key.ToLowerInvariant(), null, ct))
            return Result.Failure<CustomFieldDto>(
                ErrorType.Conflict, CustomFieldErrors.MsgKeyDup,
                new[] { new ResultError(CustomFieldErrors.KeyDuplicated, CustomFieldErrors.MsgKeyDup, Field: "key") });

        var f = new Domain.CustomField(request.Key, request.Name, (CustomFieldType)request.Type,
            request.Description, request.IsSearchable, request.ConfigJson);
        await _repo.AddAsync(f, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("CustomField created Id={Id} Key={Key}", f.Id, f.Key);
        return Result.Success(Mappers.ToDto(f), "field.created.success", new { name = f.Name });
    }

    public async Task<Result<CustomFieldDto>> UpdateAsync(Guid id, UpdateCustomFieldRequest request, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");

        f.Rename(request.Name);
        f.UpdateDescription(request.Description);
        f.SetSearchable(request.IsSearchable);
        if (request.ConfigJson is not null) f.UpdateConfig(request.ConfigJson);
        _repo.Update(f);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.updated.success");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var f = await _repo.GetByIdAsync(id, ct);
        if (f is null) return Result.Failure(ErrorType.NotFound, "field.not_found");
        f.EnsureCanDelete();
        _repo.Remove(f);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "field.deleted.success");
    }

    public async Task<Result<CustomFieldDto>> AddOptionAsync(Guid id, AddOptionRequest request, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");
        f.AddOption(request.Value, request.Label, request.ParentOptionId, request.Order);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.option.added");
    }

    public async Task<Result<CustomFieldDto>> UpdateOptionAsync(Guid id, Guid optionId, UpdateOptionRequest request, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");
        f.UpdateOption(optionId, request.Value, request.Label, request.Order);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.option.updated");
    }

    public async Task<Result<CustomFieldDto>> RemoveOptionAsync(Guid id, Guid optionId, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");
        f.RemoveOption(optionId);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.option.removed");
    }

    public async Task<Result<CustomFieldDto>> AddContextAsync(Guid id, AddContextRequest request, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");
        f.AddContext(request.Name, request.IsGlobal, request.IsRequired, request.DefaultValueJson,
            request.ProjectIds, request.IssueTypeIds);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.context.added");
    }

    public async Task<Result<CustomFieldDto>> RemoveContextAsync(Guid id, Guid contextId, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");
        f.RemoveContext(contextId);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.context.removed");
    }
}
