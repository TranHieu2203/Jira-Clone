using AuditLog.Application;
using AuditLog.Application.Repositories;
using AuditLog.Infrastructure;
using BB.Persistence;
using BB.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLog.Api;

public static class AuditLogModule
{
    public static IServiceCollection AddAuditLogModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AuditLogDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(AuditLogDbContext).Assembly.GetName().Name));

        services.AddScoped<IAuditUnitOfWork, AuditUnitOfWork>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();

        // Cross-cutting interface từ BB.Security — bind sang EF impl.
        services.AddScoped<IAuditLogger, EfAuditLogger>();

        return services;
    }
}
