using BB.Common;
using BB.Security;
using Issue.Application.Repositories;
using Issue.Domain;
using Microsoft.Extensions.Logging;

namespace Issue.Application;

public sealed class SavedFilterService : ISavedFilterService
{
    private readonly ISavedFilterRepository _repo;
    private readonly IIssueUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SavedFilterService> _logger;

    public SavedFilterService(
        ISavedFilterRepository repo,
        IIssueUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<SavedFilterService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SavedFilterDto>>> ListMineAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<SavedFilterDto>>(ErrorType.Unauthorized, "auth.required");

        var list = await _repo.ListVisibleToUserAsync(_currentUser.UserId.Value, ct);
        return Result.Success<IReadOnlyList<SavedFilterDto>>(list.Select(ToDto).ToList());
    }

    public async Task<Result<SavedFilterDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SavedFilterDto>(ErrorType.Unauthorized, "auth.required");

        var f = await _repo.GetByIdAsync(id, ct);
        if (f is null) return Result.Failure<SavedFilterDto>(ErrorType.NotFound, "saved_filter.not_found");

        // Quyền xem: là chủ hoặc filter shared. Nếu không, trả 404 để không leak existence.
        if (f.OwnerUserId != _currentUser.UserId.Value && !f.IsShared)
            return Result.Failure<SavedFilterDto>(ErrorType.NotFound, "saved_filter.not_found");

        return Result.Success(ToDto(f));
    }

    public async Task<Result<SavedFilterDto>> CreateAsync(CreateSavedFilterRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SavedFilterDto>(ErrorType.Unauthorized, "auth.required");

        SavedFilter f;
        try
        {
            f = new SavedFilter(_currentUser.UserId.Value, request.Name, request.Jql, request.Description, request.IsShared);
        }
        catch (DomainException dex)
        {
            return Result.Failure<SavedFilterDto>(ErrorType.Validation, dex.MessageKey);
        }

        await _repo.AddAsync(f, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("SavedFilter created Id={Id} Owner={Owner}", f.Id, f.OwnerUserId);
        return Result.Success(ToDto(f), "saved_filter.created");
    }

    public async Task<Result<SavedFilterDto>> UpdateAsync(Guid id, UpdateSavedFilterRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SavedFilterDto>(ErrorType.Unauthorized, "auth.required");

        var f = await _repo.GetByIdAsync(id, ct);
        if (f is null) return Result.Failure<SavedFilterDto>(ErrorType.NotFound, "saved_filter.not_found");

        // Chặn cross-user: nếu không phải owner và không shared → 404 (giấu existence).
        if (f.OwnerUserId != _currentUser.UserId.Value && !f.IsShared)
            return Result.Failure<SavedFilterDto>(ErrorType.NotFound, "saved_filter.not_found");

        try
        {
            f.EnsureCanModify(_currentUser.UserId.Value);
            f.Update(request.Name, request.Jql, request.Description, request.IsShared);
        }
        catch (DomainException dex) when (dex.Code == "SAVED_FILTER_NOT_OWNER")
        {
            return Result.Failure<SavedFilterDto>(ErrorType.Forbidden, dex.MessageKey);
        }
        catch (DomainException dex)
        {
            return Result.Failure<SavedFilterDto>(ErrorType.Validation, dex.MessageKey);
        }

        _repo.Update(f);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(f), "saved_filter.updated");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        var f = await _repo.GetByIdAsync(id, ct);
        if (f is null) return Result.Failure(ErrorType.NotFound, "saved_filter.not_found");

        if (f.OwnerUserId != _currentUser.UserId.Value && !f.IsShared)
            return Result.Failure(ErrorType.NotFound, "saved_filter.not_found");

        try
        {
            f.EnsureCanModify(_currentUser.UserId.Value);
        }
        catch (DomainException dex) when (dex.Code == "SAVED_FILTER_NOT_OWNER")
        {
            return Result.Failure(ErrorType.Forbidden, dex.MessageKey);
        }

        _repo.Remove(f);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "saved_filter.deleted");
    }

    private static SavedFilterDto ToDto(SavedFilter f) =>
        new(f.Id, f.OwnerUserId, f.Name, f.Jql, f.Description, f.IsShared, f.CreatedAt, f.UpdatedAt);
}
