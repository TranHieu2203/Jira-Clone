using BB.Common;

namespace Identity.Application;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<CurrentUserDto>> MeAsync(Guid userId, CancellationToken ct = default);
}
