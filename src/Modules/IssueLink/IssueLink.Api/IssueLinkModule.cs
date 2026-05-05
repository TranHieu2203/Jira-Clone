using BB.Persistence;
using IssueLink.Application;
using IssueLink.Application.Repositories;
using IssueLink.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IssueLink.Api;

public static class IssueLinkModule
{
    public static IServiceCollection AddIssueLinkModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<IssueLinkDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(IssueLinkDbContext).Assembly.GetName().Name));

        services.AddScoped<IIssueLinkUnitOfWork, IssueLinkUnitOfWork>();
        services.AddScoped<IIssueLinkRepository, IssueLinkRepository>();
        services.AddScoped<IIssueLinkService, IssueLinkService>();

        return services;
    }
}
