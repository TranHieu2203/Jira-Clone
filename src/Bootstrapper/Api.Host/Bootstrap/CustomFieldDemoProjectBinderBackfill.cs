using CustomField.Application;
using Microsoft.EntityFrameworkCore;
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
            await binder.EnsureContextsForProjectAsync(id, ct);
    }
}
