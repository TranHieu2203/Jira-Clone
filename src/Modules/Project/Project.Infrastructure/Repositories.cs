using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Project.Application.Repositories;
using Project.Domain;

namespace Project.Infrastructure;

public sealed class WorkspaceRepository : Repository<Workspace>, IWorkspaceRepository
{
    private readonly ProjectDbContext _ctx;
    public WorkspaceRepository(ProjectDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<Workspace?> GetWithMembersAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Workspaces.Include(w => w.Members).FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task AddWorkspaceMemberInsertOnlyAsync(Guid workspaceId, Guid userId, int role, CancellationToken ct = default)
    {
        string prov = _ctx.Database.ProviderName ?? string.Empty;
        Guid newId = Guid.NewGuid();
        DateTimeOffset joinedAt = DateTimeOffset.UtcNow;
        if (prov.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            await _ctx.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO project.workspace_members (id, workspace_id, user_id, role, joined_at)
                VALUES ({newId}, {workspaceId}, {userId}, {role}, {joinedAt})
                """,
                ct);
            return;
        }

        throw new NotSupportedException($"AddWorkspaceMemberInsertOnlyAsync: provider '{prov}' — use domain AddMember path.");
    }

    public Task<Workspace?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _ctx.Workspaces.Include(w => w.Members).FirstOrDefaultAsync(w => w.Slug == slug.ToLower(), ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        var q = _ctx.Workspaces.AsNoTracking().Where(w => w.Slug == slug.ToLower());
        if (excludeId.HasValue) q = q.Where(w => w.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Workspace>> ListByMemberAsync(Guid userId, CancellationToken ct = default) =>
        await _ctx.Workspaces.AsNoTracking()
            .Include(w => w.Members)
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
}

public sealed class ProjectRepository : Repository<Domain.Project>, IProjectRepository
{
    private readonly ProjectDbContext _ctx;
    public ProjectRepository(ProjectDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<Domain.Project?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Projects
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Domain.Project>> ListWithDetailsByKeyForMemberAsync(Guid userId, string key, CancellationToken ct = default)
    {
        string k = key.ToUpperInvariant();
        return await _ctx.Projects
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .Where(p => p.Key == k && p.Members.Any(m => m.UserId == userId))
            .OrderBy(p => p.WorkspaceId)
            .ToListAsync(ct);
    }

    public Task<Domain.Project?> GetByKeyAsync(Guid workspaceId, string key, CancellationToken ct = default) =>
        _ctx.Projects
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.Key == key.ToUpper(), ct);

    public Task<bool> KeyExistsAsync(Guid workspaceId, string key, Guid? excludeId = null, CancellationToken ct = default)
    {
        var q = _ctx.Projects.AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId && p.Key == key.ToUpper());
        if (excludeId.HasValue) q = q.Where(p => p.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Project>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default) =>
        await _ctx.Projects.AsNoTracking()
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Domain.Project>> ListByMemberAsync(Guid userId, CancellationToken ct = default) =>
        await _ctx.Projects.AsNoTracking()
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListProjectIdsByMemberAsync(Guid userId, CancellationToken ct = default) =>
        await _ctx.Projects.AsNoTracking()
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .Select(p => p.Id)
            .ToListAsync(ct);

    public Task<bool> IsUserMemberOfProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default) =>
        _ctx.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.Members.Any(m => m.UserId == userId), ct);

    public Task<IssueType?> GetIssueTypeByIdAsync(Guid issueTypeId, CancellationToken ct = default) =>
        _ctx.IssueTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == issueTypeId, ct);
}

public sealed class ProjectUnitOfWork : UnitOfWork<ProjectDbContext>, IProjectUnitOfWork
{
    public ProjectUnitOfWork(ProjectDbContext ctx) : base(ctx) { }
}
