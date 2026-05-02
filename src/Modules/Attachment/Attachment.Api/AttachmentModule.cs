using Attachment.Application;
using Attachment.Application.Repositories;
using Attachment.Infrastructure;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Attachment.Api;

public static class AttachmentModule
{
    public static IServiceCollection AddAttachmentModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AttachmentDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(AttachmentDbContext).Assembly.GetName().Name));

        services.AddScoped<IAttachmentUnitOfWork, AttachmentUnitOfWork>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        return services;
    }
}
