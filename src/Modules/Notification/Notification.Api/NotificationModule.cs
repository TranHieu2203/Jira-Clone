using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application;
using Notification.Application.Handlers;
using Notification.Application.Infrastructure;
using Notification.Application.Repositories;
using Notification.Infrastructure;
using Notification.Infrastructure.Email;

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

        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<IEmailUserPreferenceRepository, EmailUserPreferenceRepository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailPreferenceService, EmailPreferenceService>();
        services.AddScoped<IEventEmailDispatcher, EventEmailDispatcher>();
        services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();

        services.Configure<EmailOptions>(cfg.GetSection("Email"));
        services.Configure<ResendOptions>(cfg.GetSection("Resend"));
        services.AddHttpClient<ResendEmailSender>();
        services.AddScoped<IEmailSender, ResendEmailSender>();

        services.AddScoped<IEventHandler<IssueAssigneeChangedIntegrationEvent>, IssueAssigneeChangedNotificationHandler>();
        services.AddScoped<IEventHandler<IssueStatusChangedIntegrationEvent>, IssueStatusChangedNotificationHandler>();
        services.AddScoped<IEventHandler<CommentAddedIntegrationEvent>, CommentAddedNotificationHandler>();

        return services;
    }
}
