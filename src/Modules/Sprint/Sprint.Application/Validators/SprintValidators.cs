using FluentValidation;

namespace Sprint.Application.Validators;

public sealed class CreateSprintRequestValidator : AbstractValidator<CreateSprintRequest>
{
    public CreateSprintRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public sealed class UpdateSprintRequestValidator : AbstractValidator<UpdateSprintRequest>
{
    public UpdateSprintRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public sealed class ReorderSprintIssuesRequestValidator : AbstractValidator<ReorderSprintIssuesRequest>
{
    public ReorderSprintIssuesRequestValidator()
    {
        RuleFor(x => x.IssueIds).NotEmpty();
    }
}
