using BB.Common;
using Project.Domain.Events;

namespace CustomField.Application.Handlers;

/// <summary>
/// Khi tạo project: gắn layout field demo theo project (không dùng global context).
/// </summary>
public sealed class ProjectCustomFieldProvisioningHandler : IDomainEventHandler<ProjectCreated>
{
    private readonly IDemoCustomFieldProjectBinder _binder;

    public ProjectCustomFieldProvisioningHandler(IDemoCustomFieldProjectBinder binder) => _binder = binder;

    public Task HandleAsync(ProjectCreated @event, CancellationToken ct = default) =>
        _binder.EnsureContextsForProjectAsync(@event.ProjectId, ct);
}
