using System.Linq;
using CustomField.Application;
using CustomField.Application.Repositories;
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
            var f = await _repo.GetByKeyAsync(key, ct);
            if (f is null)
            {
                _logger.LogDebug("Demo custom field {Key} not found; skip binding project {ProjectId}", key, projectId);
                continue;
            }

            if (f.Contexts.Any(c => !c.IsGlobal && c.ProjectIds.Contains(projectId)))
                continue;

            f.AddContext("Project", isGlobal: false, isRequired: false, defaultValueJson: null,
                projectIds: new[] { projectId }, issueTypeIds: null, displayOrder: order);
            _repo.Update(f);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
