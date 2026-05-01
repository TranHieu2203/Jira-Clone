using BB.Persistence;
using Workflow.Domain;

namespace Workflow.Application.Repositories;

public interface IWorkflowRepository : IRepository<Domain.Workflow>
{
    Task<Domain.Workflow?> GetWithDetailsAsync(Guid workflowId, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Workflow>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Workflow>> ListTemplatesAsync(CancellationToken ct = default);
    Task<bool> KeyExistsAsync(Guid? projectId, string key, Guid? excludeId = null, CancellationToken ct = default);
}

public interface IWorkflowSchemeRepository : IRepository<WorkflowScheme>
{
    Task<WorkflowScheme?> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
}

public interface IIssueStatusHistoryRepository : IRepository<IssueStatusHistory>
{
    Task<IReadOnlyList<IssueStatusHistory>> ListByIssueAsync(Guid issueId, CancellationToken ct = default);
}

public interface IWorkflowUnitOfWork : IUnitOfWork { }
