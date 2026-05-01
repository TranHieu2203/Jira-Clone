namespace Sample.Application;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    string? Description);

public sealed record UpdateProductRequest(
    string Name,
    decimal Price,
    string? Description,
    bool IsActive);

public sealed record ProductFilter(
    int PageIndex = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null);
