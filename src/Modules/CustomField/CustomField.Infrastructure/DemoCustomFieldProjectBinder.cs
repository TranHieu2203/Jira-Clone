using System.Linq;
using CustomField.Application;
using CustomField.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace CustomField.Infrastructure;

public sealed class DemoCustomFieldProjectBinder : IDemoCustomFieldProjectBinder
{
    private readonly ICustomFieldRepository _repo;
    private readonly ICustomFieldUnitOfWork _uow;
    private readonly CustomFieldDbContext _db;
    private readonly ILogger<DemoCustomFieldProjectBinder> _logger;

    public DemoCustomFieldProjectBinder(
        ICustomFieldRepository repo,
        ICustomFieldUnitOfWork uow,
        CustomFieldDbContext db,
        ILogger<DemoCustomFieldProjectBinder> logger)
    {
        _repo = repo;
        _uow = uow;
        _db = db;
        _logger = logger;
    }

    public async Task EnsureContextsForProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        foreach (var (key, order) in DemoCustomFieldLayout.FieldEntries)
        {
            await EnsureOneAsync(projectId, key, order, ct);
        }
    }

    private async Task EnsureOneAsync(Guid projectId, string key, int order, CancellationToken ct)
    {
        var f = await _repo.GetByKeyAsync(key, ct);
        if (f is null)
        {
            _logger.LogDebug("Demo custom field {Key} not found; skip binding project {ProjectId}", key, projectId);
            return;
        }

        if (f.Contexts.Any(c => !c.IsGlobal && c.ProjectIds.Contains(projectId)))
            return;

        var newCtx = f.AddContext("Project", isGlobal: false, isRequired: false, defaultValueJson: null,
            projectIds: new[] { projectId }, issueTypeIds: null, displayOrder: order);

        // CRITICAL FIX: EF Core DetectChanges treats new entities in collection navigation as
        // *Modified* (not Added) when their PK already has a non-default value (BaseEntity.Id =
        // Guid.NewGuid() in field initializer). Result: EF emits `UPDATE custom_field_contexts SET
        // ... WHERE id = <new-guid>` which matches 0 rows → DbUpdateConcurrencyException.
        //
        // Override the auto-detected state explicitly to Added so EF emits INSERT instead.
        _db.Entry(newCtx).State = EntityState.Added;

        await _uow.SaveChangesAsync(ct);
    }
}
