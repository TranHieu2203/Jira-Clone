using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workflow.Application;
using Workflow.Application.Engine;
using Workflow.Application.Engine.BuiltIn;
using Workflow.Application.Repositories;
using Workflow.Infrastructure;

namespace Workflow.Api;

public static class WorkflowModule
{
    public static IServiceCollection AddWorkflowModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<WorkflowDbContext>(opt => opt.UseConfiguredDatabase(
            cfg,
            migrationsAssembly: typeof(WorkflowDbContext).Assembly.GetName().Name));

        // Repos + UoW
        services.AddScoped<IWorkflowUnitOfWork, WorkflowUnitOfWork>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowSchemeRepository, WorkflowSchemeRepository>();
        services.AddScoped<IIssueStatusHistoryRepository, IssueStatusHistoryRepository>();

        // Services + Engine
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IWorkflowResolver, WorkflowResolver>();
        services.AddScoped<ITransitionStepRegistry, TransitionStepRegistry>();

        // Built-in steps
        services.AddScoped<ITransitionRule, PermissionRule>();
        services.AddScoped<ITransitionRule, UserIsAssigneeRule>();
        services.AddScoped<ITransitionRule, UserInRoleRule>();

        services.AddScoped<ITransitionValidator, FieldRequiredValidator>();
        services.AddScoped<ITransitionValidator, RegexMatchValidator>();
        services.AddScoped<ITransitionValidator, ResolutionRequiredValidator>();

        services.AddScoped<ITransitionPostFunction, AssignToCurrentUserPostFunction>();
        services.AddScoped<ITransitionPostFunction, SetFieldValuePostFunction>();
        services.AddScoped<ITransitionPostFunction, ClearFieldPostFunction>();

        return services;
    }
}
