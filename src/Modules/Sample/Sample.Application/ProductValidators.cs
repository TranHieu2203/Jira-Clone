using FluentValidation;

namespace Sample.Application;

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("PRODUCT_NAME_REQUIRED").WithMessage("validation.required").MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().WithErrorCode("PRODUCT_SKU_REQUIRED").WithMessage("validation.required").MaximumLength(64);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithErrorCode("PRODUCT_PRICE_INVALID").WithMessage("product.price.invalid");
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("PRODUCT_NAME_REQUIRED").WithMessage("validation.required").MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithErrorCode("PRODUCT_PRICE_INVALID").WithMessage("product.price.invalid");
    }
}
