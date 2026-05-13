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

        // Phase 7: Syncfusion DocIO cho mail-merge (export Word 2003 XML / Docx).
        // Import (Phase 6) vẫn delegate sang OpenXmlDocumentConversionService — lightweight, không cần license.
        // Cần register cả 2: composite Syncfusion service được Inject vào ITemplateService/ISubmissionService.
        services.AddScoped<OpenXmlDocumentConversionService>();
        services.AddScoped<IDocumentConversionService, SyncfusionDocumentConversionService>();

        services.AddValidatorsFromAssemblyContaining<CreateMetadataValidator>();
        return services;
    }
}
