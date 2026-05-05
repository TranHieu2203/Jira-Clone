using BB.Persistence;
using BB.Security;
using FluentValidation;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Api;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<IdentityDbContext>(opt => opt.UseConfiguredDatabase(
            cfg,
            migrationsAssembly: typeof(IdentityDbContext).Assembly.GetName().Name));

        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserSearchService, UserSearchService>();
        services.AddScoped<IUserNameLookup, UserNameLookup>();
        services.AddScoped<IUserEmailLookup, UserEmailLookup>();
        services.AddValidatorsFromAssemblyContaining<LoginValidator>();
        return services;
    }

    public static async Task SeedIdentityAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        IConfiguration cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await IdentitySeeder.SeedAsync(db, hasher, cfg, ct);
    }
}
