using BB.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Application;
using Sample.Infrastructure;

namespace Sample.Api;

public static class SampleModule
{
    public static IServiceCollection AddSampleModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<SampleDbContext>(opt => opt.UseConfiguredDatabase(
            cfg,
            migrationsAssembly: typeof(SampleDbContext).Assembly.GetName().Name));

        services.AddScoped<ISampleUnitOfWork, SampleUnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductService, ProductService>();
        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

        return services;
    }
}
