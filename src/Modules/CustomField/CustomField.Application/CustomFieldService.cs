using BB.Common;
using BB.Security;
using CustomField.Application.Repositories;
using CustomField.Domain;
using Microsoft.Extensions.Logging;

namespace CustomField.Application;

public sealed class CustomFieldService : ICustomFieldService
{
    private readonly ICustomFieldRepository _repo;
    private readonly ICustomFieldUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionChecker _permissions;
    private readonly ILogger<CustomFieldService> _logger;

    public CustomFieldService(ICustomFieldRepository repo, ICustomFieldUnitOfWork uow, ICurrentUser currentUser, IPermissionChecker permissions, ILogger<CustomFieldService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _permissions = permissions;
        _logger = logger;
    }

    /// <summary>
    /// AddContext có thể bind nhiều project — yêu cầu user có ProjectAdminField trên TẤT CẢ project được bind
    /// (tránh leak field vào project user không quản lý). Global context (IsGlobal=true) hoặc context không
    /// có ProjectIds → bỏ qua check (admin-only path).
    /// </summary>
    private async Task<Result> EnsureCanBindContextAsync(IReadOnlyList<Guid>? projectIds, CancellationToken ct)
    {
        if (projectIds is null || projectIds.Count == 0) return Result.Success();
        foreach (var pid in projectIds)
        {
            var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, pid, PermissionKeys.ProjectAdminField, ct);
            if (perm.IsFailure) return perm;
        }
        return Result.Success();
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

        // R2: chỉ Project Admin (field) được bind field vào project.
        var perm = await EnsureCanBindContextAsync(request.ProjectIds, ct);
        if (perm.IsFailure) return Result.Failure<CustomFieldDto>(perm);

        f.AddContext(request.Name, request.IsGlobal, request.IsRequired, request.DefaultValueJson,
            request.ProjectIds, request.IssueTypeIds, request.DisplayOrder ?? 0);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.context.added");
    }

    public async Task<Result<CustomFieldDto>> RemoveContextAsync(Guid id, Guid contextId, CancellationToken ct = default)
    {
        var f = await _repo.GetWithDetailsAsync(id, ct);
        if (f is null) return Result.Failure<CustomFieldDto>(ErrorType.NotFound, "field.not_found");

        // R2: tìm context đang bị remove → check permission cho project bị bind.
        var ctxToRemove = f.Contexts.FirstOrDefault(c => c.Id == contextId);
        if (ctxToRemove is not null)
        {
            var perm = await EnsureCanBindContextAsync(ctxToRemove.ProjectIds, ct);
            if (perm.IsFailure) return Result.Failure<CustomFieldDto>(perm);
        }

        f.RemoveContext(contextId);
        _repo.Update(f); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(f), "field.context.removed");
    }
}
