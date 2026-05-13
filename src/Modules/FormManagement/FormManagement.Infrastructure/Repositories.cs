using BB.Persistence;
using FormManagement.Application.Repositories;
using FormManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormManagement.Infrastructure;

public sealed class MetadataRepository : Repository<MetadataDef>, IMetadataRepository
{
    private readonly FormManagementDbContext _ctx;
    public MetadataRepository(FormManagementDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<MetadataDef?> GetByValueAsync(string value, CancellationToken ct = default) =>
        _ctx.Metadata.FirstOrDefaultAsync(m => m.Value == value.ToUpper() && !m.IsDeleted, ct);

    public Task<bool> ValueExistsAsync(string value, Guid? excludeId = null, CancellationToken ct = default)
    {
        var q = _ctx.Metadata.AsNoTracking().Where(m => m.Value == value.ToUpper() && !m.IsDeleted);
        if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<MetadataDef>> SearchAsync(string? keyword, string? group, CancellationToken ct = default)
    {
        var q = _ctx.Metadata.AsNoTracking().Where(m => !m.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            var kUpper = k.ToUpper();
            // EF.Functions.Like vẫn provider-neutral, vẫn dùng case-insensitive search ở mức database.
            q = q.Where(m => EF.Functions.Like(m.Value, $"%{kUpper}%") || EF.Functions.Like(m.Label, $"%{k}%"));
        }
        if (!string.IsNullOrWhiteSpace(group))
            q = q.Where(m => m.FieldGroup == group);

        return await q.OrderBy(m => m.FieldGroup).ThenBy(m => m.Value).ToListAsync(ct);
    }
}

public sealed class TemplateRepository : Repository<DocumentTemplate>, ITemplateRepository
{
    private readonly FormManagementDbContext _ctx;
    public TemplateRepository(FormManagementDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<DocumentTemplate?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _ctx.Templates.FirstOrDefaultAsync(t => t.Code == code.ToUpper() && !t.IsDeleted, ct);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var q = _ctx.Templates.AsNoTracking().Where(t => t.Code == code.ToUpper() && !t.IsDeleted);
        if (excludeId.HasValue) q = q.Where(t => t.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentTemplate>> SearchAsync(string? keyword, TemplateStatus? status, string? category, CancellationToken ct = default)
    {
        var q = _ctx.Templates.AsNoTracking().Where(t => !t.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(t => EF.Functions.Like(t.Code, $"%{k.ToUpper()}%") || EF.Functions.Like(t.Name, $"%{k}%"));
        }
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(t => t.Category == category);

        return await q.OrderByDescending(t => t.CreatedAt).Take(200).ToListAsync(ct);
    }
}

public sealed class SubmissionRepository : Repository<Submission>, ISubmissionRepository
{
    private readonly FormManagementDbContext _ctx;
    public SubmissionRepository(FormManagementDbContext ctx) : base(ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<Submission>> ListByTemplateAsync(Guid templateId, int take = 50, CancellationToken ct = default) =>
        await _ctx.Submissions.AsNoTracking()
            .Where(s => s.TemplateId == templateId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
}

public sealed class FormManagementUnitOfWork : UnitOfWork<FormManagementDbContext>, IFormManagementUnitOfWork
{
    public FormManagementUnitOfWork(FormManagementDbContext ctx) : base(ctx) { }
}
