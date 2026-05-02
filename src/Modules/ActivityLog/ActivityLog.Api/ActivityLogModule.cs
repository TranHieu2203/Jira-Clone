using ActivityLog.Application;
using ActivityLog.Application.Handlers;
using ActivityLog.Application.Repositories;
using ActivityLog.Infrastructure;
using BB.Common;
using BB.Persistence;
using Comment.Domain.Events;
using Issue.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityLog.Api;

public static class ActivityLogModule
{
    public static IServiceCollection AddActivityLogModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<ActivityLogDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(ActivityLogDbContext).Assembly.GetName().Name));

        services.AddScoped<IActivityLogUnitOfWork, ActivityLogUnitOfWork>();
        services.AddScoped<IActivityEntryRepository, ActivityEntryRepository>();
        services.AddScoped<IActivityLogService, ActivityLogService>();

        services.AddScoped<IDomainEventHandler<IssueCreated>, IssueCreatedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueUpdated>, IssueUpdatedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueAssigneeChanged>, IssueAssigneeChangedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueStatusChanged>, IssueStatusChangedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueParentChanged>, IssueParentChangedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueWatcherAdded>, IssueWatcherAddedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueWatcherRemoved>, IssueWatcherRemovedActivityHandler>();
        services.AddScoped<IDomainEventHandler<IssueArchived>, IssueArchivedActivityHandler>();
        services.AddScoped<IDomainEventHandler<CommentAdded>, CommentAddedActivityHandler>();
        services.AddScoped<IDomainEventHandler<CommentEdited>, CommentEditedActivityHandler>();
        services.AddScoped<IDomainEventHandler<CommentDeleted>, CommentDeletedActivityHandler>();

        return services;
    }
}
