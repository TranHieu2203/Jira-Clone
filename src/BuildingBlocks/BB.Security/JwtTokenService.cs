using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BB.Security;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt, DateTimeOffset RefreshExpiresAt);

public interface IJwtTokenService
{
    TokenPair Generate(Guid userId, string userName, IEnumerable<string> roles);
    string GenerateRefreshToken();
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opts;

    public JwtTokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public TokenPair Generate(Guid userId, string userName, IEnumerable<string> roles)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExp = now.AddMinutes(_opts.AccessTokenMinutes);
        var refreshExp = now.AddDays(_opts.RefreshTokenDays);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExp.UtcDateTime,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenPair(access, GenerateRefreshToken(), accessExp, refreshExp);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
