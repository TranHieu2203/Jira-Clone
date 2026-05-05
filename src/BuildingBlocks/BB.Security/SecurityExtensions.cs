using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BB.Security;

public static class SecurityExtensions
{
    /// <summary>
    /// Min length cho JWT HMAC-SHA256 key — RFC 7518 yêu cầu key ≥ 256 bits (32 bytes UTF-8).
    /// </summary>
    public const int MinSigningKeyBytes = 32;

    public static IServiceCollection AddBbSecurity(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<JwtOptions>(cfg.GetSection(JwtOptions.SectionName));
        var opts = cfg.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                   ?? throw new InvalidOperationException("Missing Jwt configuration section");

        // C4: fail-fast nếu signing key trống hoặc quá ngắn — production cần set qua env var `Jwt__SigningKey`
        // hoặc `dotnet user-secrets` (dev). Không fallback default để tránh deploy sai.
        if (string.IsNullOrWhiteSpace(opts.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is empty. Set env var Jwt__SigningKey (production) or use 'dotnet user-secrets set Jwt:SigningKey <value>' (dev).");
        }
        if (Encoding.UTF8.GetByteCount(opts.SigningKey) < MinSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey too short — needs ≥ {MinSigningKeyBytes} bytes (256 bits) for HS256. Use a random secret (e.g. `openssl rand -base64 48`).");
        }

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = opts.Issuer,
                    ValidAudience = opts.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        Microsoft.Extensions.Primitives.StringValues accessToken = context.Request.Query["access_token"];
                        string path = context.HttpContext.Request.Path.Value ?? string.Empty;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
