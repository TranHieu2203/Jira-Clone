using System.Linq;
using CustomField.Application;
using CustomField.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomField.Infrastructure;

public sealed class DemoCustomFieldProjectBinder : IDemoCustomFieldProjectBinder
{
    private readonly ICustomFieldRepository _repo;
    private readonly ICustomFieldUnitOfWork _uow;
    private readonly ILogger<DemoCustomFieldProjectBinder> _logger;

    public DemoCustomFieldProjectBinder(
        ICustomFieldRepository repo,
        ICustomFieldUnitOfWork uow,
        ILogger<DemoCustomFieldProjectBinder> logger)
    {
        _repo = repo;
        _uow = uow;
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
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var f = await _repo.GetByKeyAsync(key, ct);
            if (f is null)
            {
                _logger.LogDebug("Demo custom field {Key} not found; skip binding project {ProjectId}", key, projectId);
                return;
            }

            if (f.Contexts.Any(c => !c.IsGlobal && c.ProjectIds.Contains(projectId)))
                return;

            f.AddContext("Project", isGlobal: false, isRequired: false, defaultValueJson: null,
                projectIds: new[] { projectId }, issueTypeIds: null, displayOrder: order);

            try
            {
                await _uow.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency when binding demo custom field {Key} for project {ProjectId}. Retrying...", key, projectId);
            }
        }

        _logger.LogWarning("Skip binding demo custom field {Key} for project {ProjectId} after concurrency retries", key, projectId);
    }
}
