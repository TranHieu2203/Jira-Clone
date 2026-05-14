using BB.Persistence;
using FluentValidation;
using FormManagement.Application;
using FormManagement.Application.Repositories;
using FormManagement.Application.Services;
using FormManagement.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FormManagement.Api;

public static class FormManagementModule
{
    public static IServiceCollection AddFormManagementModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<FormManagementDbContext>(opt => opt.UseConfiguredDatabase(
            cfg,
            migrationsAssembly: typeof(FormManagementDbContext).Assembly.GetName().Name));

        services.AddScoped<IFormManagementUnitOfWork, FormManagementUnitOfWork>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();

        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        // OpenXml + Clippit cho cả detect placeholder + mail-merge. Không Syncfusion → không license.
        // FE OnlyOffice DocServer render DOCX native, BE chỉ cần parse + mail-merge bytes.
        services.AddScoped<IDocumentConversionService, OpenXmlDocumentConversionService>();

        services.AddValidatorsFromAssemblyContaining<CreateMetadataValidator>();
        return services;
    }
}
