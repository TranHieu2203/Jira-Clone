using BB.Common;
using Identity.Domain;

namespace Identity.Application;

public sealed class UserSearchService : IUserSearchService
{
    private readonly IUserRepository _repo;

    public UserSearchService(IUserRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<UserSummaryDto>>> SearchAsync(string? query, int take, CancellationToken ct = default)
    {
        int limit = Math.Clamp(take, 1, 50);
        IReadOnlyList<User> users = await _repo.SearchActiveUsersAsync(query, limit, ct);
        IReadOnlyList<UserSummaryDto> dtos = users.Select(u => new UserSummaryDto(u.Id, u.UserName, u.DisplayName)).ToList();
        return Result.Success<IReadOnlyList<UserSummaryDto>>(dtos);
    }

    public async Task<Result<UserSummaryDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        User? u = await _repo.GetByIdAsync(id, ct);
        if (u is null || !u.IsActive)
            return Result.Failure<UserSummaryDto>(ErrorType.NotFound, "user.not_found");
        return Result.Success(new UserSummaryDto(u.Id, u.UserName, u.DisplayName));
    }
}
