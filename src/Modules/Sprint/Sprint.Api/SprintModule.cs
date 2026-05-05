using BB.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sprint.Application;
using Sprint.Application.Repositories;
using Sprint.Application.Validators;
using Sprint.Infrastructure;

namespace Sprint.Api;

public static class SprintModule
{
    public static IServiceCollection AddSprintModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<SprintDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(SprintDbContext).Assembly.GetName().Name));

        services.AddScoped<ISprintUnitOfWork, SprintUnitOfWork>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintService, SprintService>();

        services.AddValidatorsFromAssemblyContaining<CreateSprintRequestValidator>();

        return services;
    }
}
