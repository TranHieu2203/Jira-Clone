using BB.Persistence;
using Issue.Application;
using Issue.Application.Repositories;
using Issue.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Issue.Api;

public static class IssueModule
{
    public static IServiceCollection AddIssueModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<IssueDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(IssueDbContext).Assembly.GetName().Name));

        services.AddScoped<IIssueUnitOfWork, IssueUnitOfWork>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IIssueNotificationSnapshotReader, IssueNotificationSnapshotReader>();
        services.AddScoped<IIssueRealtimeNotifier, NoOpIssueRealtimeNotifier>();
        services.AddScoped<IIssueAccessGuard, IssueAccessGuard>();
        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<ISavedFilterRepository, SavedFilterRepository>();
        services.AddScoped<ISavedFilterService, SavedFilterService>();

        return services;
    }
}
