using BB.Common;

namespace Sample.Application;

public interface IProductService
{
    Task<Result<PagedList<ProductDto>>> SearchAsync(ProductFilter filter, CancellationToken ct = default);
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
