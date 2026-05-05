using BB.Common;

namespace Issue.Application;

public interface ISavedFilterService
{
    /// <summary>Filter của current user + filter shared.</summary>
    Task<Result<IReadOnlyList<SavedFilterDto>>> ListMineAsync(CancellationToken ct = default);

    Task<Result<SavedFilterDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<SavedFilterDto>> CreateAsync(CreateSavedFilterRequest request, CancellationToken ct = default);
    Task<Result<SavedFilterDto>> UpdateAsync(Guid id, UpdateSavedFilterRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
