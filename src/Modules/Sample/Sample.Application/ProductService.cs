using BB.Common;
using BB.Persistence;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Sample.Domain;

namespace Sample.Application;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly ISampleUnitOfWork _uow;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository repo,
        ISampleUnitOfWork uow,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        ILogger<ProductService> logger)
    {
        _repo = repo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<Result<PagedList<ProductDto>>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var page = await _repo.SearchAsync(filter, ct);
        var dtoItems = page.Items.Select(ToDto).ToList();
        return Result.Success(new PagedList<ProductDto>(dtoItems, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure<ProductDto>(ErrorType.NotFound, "product.not_found");
        }
        return Result.Success(ToDto(entity));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        if (await _repo.SkuExistsAsync(request.Sku, null, ct))
        {
            return Result.Failure<ProductDto>(
                ErrorType.Conflict,
                "product.sku_duplicated",
                new[] { new ResultError("PRODUCT_SKU_DUPLICATED", "product.sku_duplicated", "sku") });
        }

        var product = new Product(request.Name, request.Sku, request.Price, request.Description);
        await _repo.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product created. Id={ProductId} Sku={Sku}", product.Id, product.Sku);

        return Result.Success(
            ToDto(product),
            messageKey: "product.created.success",
            messageArgs: new { name = product.Name });
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var product = await _repo.GetByIdAsync(id, ct);
        if (product is null)
        {
            return Result.Failure<ProductDto>(ErrorType.NotFound, "product.not_found");
        }

        product.Update(request.Name, request.Price, request.Description, request.IsActive);
        _repo.Update(product);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(
            ToDto(product),
            messageKey: "product.updated.success",
            messageArgs: new { name = product.Name });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(id, ct);
        if (product is null)
        {
            return Result.Failure(ErrorType.NotFound, "product.not_found");
        }
        _repo.Remove(product);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "product.deleted.success");
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Sku, p.Price, p.Description, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
