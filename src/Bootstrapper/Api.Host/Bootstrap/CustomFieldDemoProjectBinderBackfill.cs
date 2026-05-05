using CustomField.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Infrastructure;

namespace Api.Host.Bootstrap;

/// <summary>
/// Gắn context demo cho mọi project đã tồn tại (DB cũ / trước khi có handler).
/// </summary>
public static class CustomFieldDemoProjectBinderBackfill
{
    public static async Task RunAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = sp.CreateAsyncScope();
        ProjectDbContext projectDb = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        IDemoCustomFieldProjectBinder binder = scope.ServiceProvider.GetRequiredService<IDemoCustomFieldProjectBinder>();
        List<Guid> ids = await projectDb.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);
        foreach (Guid id in ids)
        {
            try
            {
                await binder.EnsureContextsForProjectAsync(id, ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Backfill là best-effort; không được làm crash app khi DB đã có data cũ.
                // Những project bị skip vẫn sẽ được gắn context khi có handler ProjectCreated chạy về sau.
                var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                    .CreateLogger("Bootstrap");
                logger.LogWarning(ex, "Skip demo custom field backfill for project {ProjectId} due to concurrency", id);
            }
        }
    }
}
