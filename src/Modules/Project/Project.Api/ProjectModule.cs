using BB.Persistence;
using BB.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Repositories;
using Project.Application.Security;
using Project.Infrastructure;

namespace Project.Api;

public static class ProjectModule
{
    public static IServiceCollection AddProjectModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<ProjectDbContext>(opt => opt.UseConfiguredDatabase(
            cfg,
            migrationsAssembly: typeof(ProjectDbContext).Assembly.GetName().Name));

        services.AddScoped<IProjectUnitOfWork, ProjectUnitOfWork>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IIssueTypeReader, IssueTypeReader>();
        services.AddScoped<IIssueNumberAllocator, IssueNumberAllocator>();

        // Permission checker — module Project nắm dữ liệu role nên đăng ký impl tại đây.
        // Các module khác (Workflow, Issue) inject IPermissionChecker không phụ thuộc Project.Infrastructure.
        services.AddScoped<IPermissionChecker, RoleBasedPermissionChecker>();

        return services;
    }
}
