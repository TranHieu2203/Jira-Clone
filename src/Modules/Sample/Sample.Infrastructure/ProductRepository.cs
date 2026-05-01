using BB.Common;
using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Sample.Application;
using Sample.Domain;

namespace Sample.Infrastructure;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly SampleDbContext _ctx;

    public ProductRepository(SampleDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken ct = default)
    {
        var q = _ctx.Products.AsNoTracking().Where(p => p.Sku == sku);
        if (excludeId.HasValue) q = q.Where(p => p.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }

    public async Task<PagedList<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        IQueryable<Product> q = _ctx.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }
        if (filter.IsActive.HasValue)
        {
            q = q.Where(p => p.IsActive == filter.IsActive.Value);
        }

        var total = await q.LongCountAsync(ct);
        var page = Math.Max(filter.PageIndex, 1);
        var size = Math.Max(filter.PageSize, 1);
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        return new PagedList<Product>(items, total, page, size);
    }
}
