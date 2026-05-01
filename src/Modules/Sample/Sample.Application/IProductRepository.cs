using BB.Common;
using BB.Persistence;
using Sample.Domain;

namespace Sample.Application;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken ct = default);
    Task<PagedList<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default);
}
