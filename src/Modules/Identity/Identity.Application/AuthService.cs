using BB.Common;
using BB.Persistence;
using BB.Security;
using Identity.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IIdentityUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IIdentityUnitOfWork uow,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        IOptions<JwtOptions> jwt,
        ILogger<AuthService> logger)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
        _jwt = jwt.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByUserNameAsync(request.UserName, ct);
        if (user is null || !user.IsActive || !_hasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogInformation("Login failed for {UserName}", request.UserName);
            return Result.Failure<AuthResponse>(ErrorType.Unauthorized, "auth.invalid_credentials");
        }

        var roles = user.Roles.Select(r => r.Role.Name).ToList();
        var pair = _tokens.Generate(user.Id, user.UserName, roles);

        await _users.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = pair.RefreshToken,
            ExpiresAt = pair.RefreshExpiresAt
        }, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(
            user.Id, user.UserName, user.DisplayName, roles,
            pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var existing = await _users.GetRefreshTokenAsync(request.RefreshToken, ct);
        if (existing is null || !existing.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorType.Unauthorized, "auth.unauthorized");
        }

        var user = await _users.GetByIdAsync(existing.UserId, ct);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(ErrorType.Unauthorized, "auth.unauthorized");
        }

        // Reload roles via a fresh query (untracked user from FindByUserNameAsync would be nicer,
        // but here we already have the tracked entity from GetByIdAsync).
        var freshUser = await _users.FindByUserNameAsync(user.UserName, ct);
        var roles = freshUser?.Roles.Select(r => r.Role.Name).ToList() ?? new List<string>();

        var pair = _tokens.Generate(user.Id, user.UserName, roles);

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.ReplacedByToken = pair.RefreshToken;

        await _users.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = pair.RefreshToken,
            ExpiresAt = pair.RefreshExpiresAt
        }, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(
            user.Id, user.UserName, user.DisplayName, roles,
            pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt));
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var existing = await _users.GetRefreshTokenAsync(refreshToken, ct);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTimeOffset.UtcNow;
            await _uow.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result<CurrentUserDto>> MeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(ErrorType.NotFound, "auth.unauthorized");
        }
        var fresh = await _users.FindByUserNameAsync(user.UserName, ct);
        var roles = fresh?.Roles.Select(r => r.Role.Name).ToList() ?? new List<string>();
        return Result.Success(new CurrentUserDto(user.Id, user.UserName, user.DisplayName, user.Email, roles));
    }
}
