using BB.Common;

namespace Sample.Domain;

public sealed class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    private Product() { }

    public Product(string name, string sku, decimal price, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("PRODUCT_NAME_REQUIRED", "validation.required");
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("PRODUCT_SKU_REQUIRED", "validation.required");
        if (price < 0) throw new DomainException("PRODUCT_PRICE_INVALID", "product.price.invalid");

        Name = name;
        Sku = sku;
        Price = price;
        Description = description;
    }

    public void Update(string name, decimal price, string? description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("PRODUCT_NAME_REQUIRED", "validation.required");
        if (price < 0) throw new DomainException("PRODUCT_PRICE_INVALID", "product.price.invalid");
        Name = name;
        Price = price;
        Description = description;
        IsActive = isActive;
    }
}
