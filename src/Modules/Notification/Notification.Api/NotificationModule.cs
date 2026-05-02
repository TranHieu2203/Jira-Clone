using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application;
using Notification.Application.Handlers;
using Notification.Application.Repositories;
using Notification.Infrastructure;

namespace Notification.Api;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<NotificationDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(NotificationDbContext).Assembly.GetName().Name));

        services.AddScoped<INotificationUnitOfWork, NotificationUnitOfWork>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IEventHandler<IssueAssigneeChangedIntegrationEvent>, IssueAssigneeChangedNotificationHandler>();
        services.AddScoped<IEventHandler<IssueStatusChangedIntegrationEvent>, IssueStatusChangedNotificationHandler>();
        services.AddScoped<IEventHandler<CommentAddedIntegrationEvent>, CommentAddedNotificationHandler>();

        return services;
    }
}
