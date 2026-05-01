namespace Identity.Application;

public sealed record LoginRequest(string UserName, string Password);
public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    Guid UserId,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessExpiresAt);

public sealed record CurrentUserDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles);
