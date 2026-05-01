using BB.Persistence;
using CustomField.Application;
using CustomField.Application.Handlers;
using CustomField.Application.Repositories;
using CustomField.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomField.Api;

public static class CustomFieldModule
{
    public static IServiceCollection AddCustomFieldModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<CustomFieldDbContext>(opt => opt.UseConfiguredDatabase(
            cfg, migrationsAssembly: typeof(CustomFieldDbContext).Assembly.GetName().Name));

        services.AddScoped<ICustomFieldUnitOfWork, CustomFieldUnitOfWork>();
        services.AddScoped<ICustomFieldRepository, CustomFieldRepository>();
        services.AddScoped<IIssueFieldValueRepository, IssueFieldValueRepository>();

        services.AddScoped<ICustomFieldService, CustomFieldService>();
        services.AddScoped<IIssueFieldValueService, IssueFieldValueService>();

        // Type handlers
        services.AddScoped<ICustomFieldTypeHandlerRegistry, CustomFieldTypeHandlerRegistry>();
        services.AddScoped<ICustomFieldTypeHandler, TextHandler>();
        services.AddScoped<ICustomFieldTypeHandler, TextAreaHandler>();
        services.AddScoped<ICustomFieldTypeHandler, NumberHandler>();
        services.AddScoped<ICustomFieldTypeHandler, DecimalHandler>();
        services.AddScoped<ICustomFieldTypeHandler, DateHandler>();
        services.AddScoped<ICustomFieldTypeHandler, DateTimeHandler>();
        services.AddScoped<ICustomFieldTypeHandler, CheckboxHandler>();
        services.AddScoped<ICustomFieldTypeHandler, UrlHandler>();
        services.AddScoped<ICustomFieldTypeHandler, SelectHandler>();
        services.AddScoped<ICustomFieldTypeHandler, MultiSelectHandler>();
        services.AddScoped<ICustomFieldTypeHandler, UserHandler>();
        services.AddScoped<ICustomFieldTypeHandler, UserMultiHandler>();
        services.AddScoped<ICustomFieldTypeHandler, LabelHandler>();

        return services;
    }
}
