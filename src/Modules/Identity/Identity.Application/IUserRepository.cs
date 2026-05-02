using BB.Persistence;
using Identity.Domain;

namespace Identity.Application;

public interface IUserRepository : IRepository<User>
{
    Task<IReadOnlyList<User>> SearchActiveUsersAsync(string? searchTerm, int take, CancellationToken ct = default);

    Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default);
    Task<User?> FindByRefreshTokenAsync(string token, CancellationToken ct = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
}
