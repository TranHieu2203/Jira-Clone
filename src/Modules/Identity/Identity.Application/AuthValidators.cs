using FluentValidation;

namespace Identity.Application;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithErrorCode("USERNAME_REQUIRED").WithMessage("validation.required");
        RuleFor(x => x.Password).NotEmpty().WithErrorCode("PASSWORD_REQUIRED").WithMessage("validation.required");
    }
}

public sealed class RefreshValidator : AbstractValidator<RefreshRequest>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithErrorCode("REFRESH_TOKEN_REQUIRED").WithMessage("validation.required");
    }
}
