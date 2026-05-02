using BB.Persistence;
using Comment.Application;
using Comment.Application.Repositories;
using Comment.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Comment.Api;

public static class CommentModule
{
    public static IServiceCollection AddCommentModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<CommentDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(CommentDbContext).Assembly.GetName().Name));

        services.AddScoped<ICommentUnitOfWork, CommentUnitOfWork>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentService, CommentService>();

        return services;
    }
}
