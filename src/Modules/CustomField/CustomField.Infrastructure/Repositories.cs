using BB.Persistence;
using CustomField.Application.Repositories;
using CustomField.Domain;
using Microsoft.EntityFrameworkCore;

namespace CustomField.Infrastructure;

public sealed class CustomFieldRepository : Repository<Domain.CustomField>, ICustomFieldRepository
{
    private readonly CustomFieldDbContext _ctx;
    public CustomFieldRepository(CustomFieldDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<Domain.CustomField?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Fields.Include(f => f.Options).Include(f => f.Contexts).FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<Domain.CustomField?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        _ctx.Fields.Include(f => f.Options).Include(f => f.Contexts).FirstOrDefaultAsync(f => f.Key == key.ToLower(), ct);

    public async Task<IReadOnlyList<Domain.CustomField>> ListAllAsync(CancellationToken ct = default) =>
        await _ctx.Fields.AsNoTracking().Include(f => f.Options).Include(f => f.Contexts)
            .OrderBy(f => f.Name).ToListAsync(ct);

    public Task<bool> KeyExistsAsync(string key, Guid? excludeId = null, CancellationToken ct = default)
    {
        var q = _ctx.Fields.AsNoTracking().Where(f => f.Key == key.ToLower());
        if (excludeId.HasValue) q = q.Where(f => f.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.CustomField>> ResolveForAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default)
    {
        // Load tất cả fields có context — filter trong-memory vì context.AppliesTo
        // dùng JSON column (không filter SQL được). Số field không quá nhiều (~50-200).
        var fields = await _ctx.Fields.AsNoTracking()
            .Include(f => f.Options)
            .Include(f => f.Contexts)
            .ToListAsync(ct);

        static int OrderFor(Domain.CustomField f, Guid pid, Guid tid) =>
            f.Contexts.Where(c => c.AppliesTo(pid, tid)).Select(c => c.DisplayOrder).DefaultIfEmpty(int.MaxValue).Min();

        return fields
            .Where(f => f.Contexts.Any(c => c.AppliesTo(projectId, issueTypeId)))
            .OrderBy(f => OrderFor(f, projectId, issueTypeId))
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void MarkContextAsAdded(CustomFieldContext context) =>
        _ctx.Entry(context).State = EntityState.Added;
}

public sealed class IssueFieldValueRepository : Repository<IssueFieldValue>, IIssueFieldValueRepository
{
    private readonly CustomFieldDbContext _ctx;
    public IssueFieldValueRepository(CustomFieldDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<IssueFieldValue>> ListByIssueAsync(Guid issueId, CancellationToken ct = default) =>
        await _ctx.IssueFieldValues.Where(v => v.IssueId == issueId).ToListAsync(ct);

    public Task<IssueFieldValue?> GetAsync(Guid issueId, Guid fieldId, CancellationToken ct = default) =>
        _ctx.IssueFieldValues.FirstOrDefaultAsync(v => v.IssueId == issueId && v.CustomFieldId == fieldId, ct);

    public async Task RemoveAllForIssueAsync(Guid issueId, CancellationToken ct = default)
    {
        var rows = await _ctx.IssueFieldValues.Where(v => v.IssueId == issueId).ToListAsync(ct);
        _ctx.IssueFieldValues.RemoveRange(rows);
    }
}

public sealed class CustomFieldUnitOfWork : UnitOfWork<CustomFieldDbContext>, ICustomFieldUnitOfWork
{
    public CustomFieldUnitOfWork(CustomFieldDbContext ctx) : base(ctx) { }
}
