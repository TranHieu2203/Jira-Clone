using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Sprint.Application.Repositories;
using Sprint.Domain;

namespace Sprint.Infrastructure;

public sealed class SprintRepository : Repository<Sprint.Domain.Sprint>, ISprintRepository
{
    private readonly SprintDbContext _ctx;

    public SprintRepository(SprintDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<Sprint.Domain.Sprint?> GetWithItemsAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Sprints.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Sprint.Domain.Sprint>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        List<Sprint.Domain.Sprint> list = await _ctx.Sprints.AsNoTracking()
            .Include(s => s.Items)
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(ct);
        return list;
    }

    public Task<Sprint.Domain.Sprint?> GetActiveForProjectAsync(Guid projectId, CancellationToken ct = default) =>
        _ctx.Sprints.Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active, ct);

    public Task<bool> HasOtherActiveSprintAsync(Guid projectId, Guid excludeSprintId, CancellationToken ct = default) =>
        _ctx.Sprints.AsNoTracking()
            .AnyAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active && s.Id != excludeSprintId, ct);

    public async Task<IReadOnlySet<Guid>> GetIssueIdsInOpenSprintsAsync(Guid projectId, CancellationToken ct = default)
    {
        List<Guid> ids = await _ctx.SprintIssues.AsNoTracking()
            .Where(si => _ctx.Sprints.Any(s =>
                s.Id == si.SprintId
                && s.ProjectId == projectId
                && (s.Status == SprintStatus.Planned || s.Status == SprintStatus.Active)))
            .Select(si => si.IssueId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<Guid?> FindOpenSprintIdForIssueAsync(Guid projectId, Guid issueId, Guid? excludeSprintId, CancellationToken ct = default)
    {
        Guid? sid = await _ctx.SprintIssues.AsNoTracking()
            .Where(si =>
                si.IssueId == issueId
                && _ctx.Sprints.Any(s =>
                    s.Id == si.SprintId
                    && s.ProjectId == projectId
                    && (s.Status == SprintStatus.Planned || s.Status == SprintStatus.Active)
                    && (!excludeSprintId.HasValue || s.Id != excludeSprintId.Value)))
            .Select(si => (Guid?)si.SprintId)
            .FirstOrDefaultAsync(ct);
        return sid;
    }

    public async Task<IReadOnlyList<SprintCommitLine>> ListCommitLinesAsync(Guid sprintId, CancellationToken ct = default)
    {
        List<SprintCommitLine> list = await _ctx.SprintCommitLines.AsNoTracking()
            .Where(l => l.SprintId == sprintId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
        return list;
    }

    public async Task AddCommitLinesAsync(IEnumerable<SprintCommitLine> lines, CancellationToken ct = default)
    {
        await _ctx.SprintCommitLines.AddRangeAsync(lines, ct);
    }
}

public sealed class SprintUnitOfWork : UnitOfWork<SprintDbContext>, ISprintUnitOfWork
{
    public SprintUnitOfWork(SprintDbContext ctx) : base(ctx) { }
}
