using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Workflow.Domain;

namespace Workflow.Infrastructure.Seed;

/// <summary>
/// Seed workflow template "SOFTWARE_SIMPLE": To Do → In Progress → Done.
/// Chỉ insert nếu chưa có template cùng key.
/// </summary>
public static class WorkflowSeeder
{
    public const string SoftwareSimpleKey = "SOFTWARE_SIMPLE";

    public static async Task SeedDefaultsAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("WorkflowSeeder");

        if (await ctx.Workflows.AnyAsync(w => w.IsTemplate && w.Key == SoftwareSimpleKey, ct))
        {
            logger.LogDebug("Default workflow template already exists. Skip.");
            return;
        }

        var wf = Domain.Workflow.CreateTemplate("Software Simple", SoftwareSimpleKey,
            "Default scrum workflow: To Do → In Progress → Done");

        var todo = wf.AddStatus("To Do", "TODO", StatusCategory.ToDo, color: "#6B7280", order: 0);
        var inProgress = wf.AddStatus("In Progress", "IN_PROGRESS", StatusCategory.InProgress, color: "#3B82F6", order: 1);
        var done = wf.AddStatus("Done", "DONE", StatusCategory.Done, color: "#10B981", order: 2);

        wf.SetInitialStatus(todo.Id);

        wf.AddTransition(todo.Id, inProgress.Id, "Start Progress");
        wf.AddTransition(inProgress.Id, done.Id, "Resolve");
        wf.AddTransition(inProgress.Id, todo.Id, "Stop Progress");
        wf.AddTransition(done.Id, inProgress.Id, "Reopen");

        // Global "Move to Done" — từ bất kỳ status nào.
        wf.AddTransition(fromStatusId: null, toStatusId: done.Id, "Force Close");

        ctx.Workflows.Add(wf);
        await ctx.SaveChangesAsync(ct);

        logger.LogInformation("Seeded default workflow template {Key} with {StatusCount} statuses, {TransitionCount} transitions",
            wf.Key, wf.Statuses.Count, wf.Transitions.Count);
    }
}
