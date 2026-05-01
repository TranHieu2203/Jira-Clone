using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Workflow.Application.Repositories;
using Workflow.Domain;

namespace Workflow.Infrastructure;

public sealed class WorkflowRepository : Repository<Domain.Workflow>, IWorkflowRepository
{
    private readonly WorkflowDbContext _ctx;

    public WorkflowRepository(WorkflowDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<Domain.Workflow?> GetWithDetailsAsync(Guid workflowId, CancellationToken ct = default) =>
        _ctx.Workflows
            .Include(w => w.Statuses)
            .Include(w => w.Transitions).ThenInclude(t => t.Rules)
            .Include(w => w.Transitions).ThenInclude(t => t.Validators)
            .Include(w => w.Transitions).ThenInclude(t => t.PostFunctions)
            .FirstOrDefaultAsync(w => w.Id == workflowId, ct);

    public async Task<IReadOnlyList<Domain.Workflow>> ListByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await _ctx.Workflows.AsNoTracking()
            .Include(w => w.Statuses)
            .Where(w => w.ProjectId == projectId)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Domain.Workflow>> ListTemplatesAsync(CancellationToken ct = default) =>
        await _ctx.Workflows.AsNoTracking()
            .Include(w => w.Statuses)
            .Where(w => w.IsTemplate)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

    public Task<bool> KeyExistsAsync(Guid? projectId, string key, Guid? excludeId = null, CancellationToken ct = default)
    {
        var q = _ctx.Workflows.AsNoTracking()
            .Where(w => w.ProjectId == projectId && w.Key == key.ToUpperInvariant());
        if (excludeId.HasValue) q = q.Where(w => w.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }
}

public sealed class WorkflowSchemeRepository : Repository<WorkflowScheme>, IWorkflowSchemeRepository
{
    private readonly WorkflowDbContext _ctx;
    public WorkflowSchemeRepository(WorkflowDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<WorkflowScheme?> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        _ctx.Schemes
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.ProjectId == projectId, ct);
}

public sealed class IssueStatusHistoryRepository : Repository<IssueStatusHistory>, IIssueStatusHistoryRepository
{
    private readonly WorkflowDbContext _ctx;
    public IssueStatusHistoryRepository(WorkflowDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<IssueStatusHistory>> ListByIssueAsync(Guid issueId, CancellationToken ct = default) =>
        await _ctx.IssueStatusHistories.AsNoTracking()
            .Where(h => h.IssueId == issueId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(ct);
}

public sealed class WorkflowUnitOfWork : UnitOfWork<WorkflowDbContext>, IWorkflowUnitOfWork
{
    public WorkflowUnitOfWork(WorkflowDbContext ctx) : base(ctx) { }
}
