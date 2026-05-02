using BB.Common;

namespace Identity.Application;

public interface IUserSearchService
{
    Task<Result<IReadOnlyList<UserSummaryDto>>> SearchAsync(string? query, int take, CancellationToken ct = default);

    Task<Result<UserSummaryDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
