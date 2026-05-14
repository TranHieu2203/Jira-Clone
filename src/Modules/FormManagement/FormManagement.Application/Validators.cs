using FluentValidation;
using FormManagement.Domain;

namespace FormManagement.Application;

public sealed class CreateMetadataValidator : AbstractValidator<CreateMetadataRequest>
{
    public CreateMetadataValidator()
    {
        RuleFor(x => x.Value).NotEmpty()
            .WithErrorCode(FormManagementErrors.MetadataValueRequired)
            .WithMessage(FormManagementErrors.MsgMetadataValueRequired);
        RuleFor(x => x.Value).Must(v => string.IsNullOrEmpty(v) || MetadataDef.ValuePattern.IsMatch(v))
            .WithErrorCode(FormManagementErrors.MetadataValueInvalid)
            .WithMessage(FormManagementErrors.MsgMetadataValueInvalid);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(255)
            .WithErrorCode(FormManagementErrors.MetadataLabelRequired)
            .WithMessage(FormManagementErrors.MsgMetadataLabelRequired);
        RuleFor(x => x.Type).IsInEnum()
            .WithErrorCode(FormManagementErrors.MetadataTypeInvalid)
            .WithMessage(FormManagementErrors.MsgMetadataTypeInvalid);
    }
}

public sealed class UpdateMetadataValidator : AbstractValidator<UpdateMetadataRequest>
{
    public UpdateMetadataValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(255)
            .WithErrorCode(FormManagementErrors.MetadataLabelRequired)
            .WithMessage(FormManagementErrors.MsgMetadataLabelRequired);
        RuleFor(x => x.Type).IsInEnum()
            .WithErrorCode(FormManagementErrors.MetadataTypeInvalid)
            .WithMessage(FormManagementErrors.MsgMetadataTypeInvalid);
    }
}

public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.Code).NotEmpty()
            .WithErrorCode(FormManagementErrors.TemplateCodeRequired)
            .WithMessage(FormManagementErrors.MsgTemplateCodeRequired);
        RuleFor(x => x.Code).Must(c => string.IsNullOrEmpty(c) || DocumentTemplate.CodePattern.IsMatch(c.Trim().ToUpperInvariant()))
            .WithErrorCode(FormManagementErrors.TemplateCodeInvalid)
            .WithMessage(FormManagementErrors.MsgTemplateCodeInvalid);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255)
            .WithErrorCode(FormManagementErrors.TemplateNameRequired)
            .WithMessage(FormManagementErrors.MsgTemplateNameRequired);
        RuleFor(x => x.DocxBase64).NotEmpty()
            .WithErrorCode(FormManagementErrors.TemplateContentRequired)
            .WithMessage(FormManagementErrors.MsgTemplateContentRequired);
    }
}

public sealed class UpdateTemplateMetadataValidator : AbstractValidator<UpdateTemplateMetadataRequest>
{
    public UpdateTemplateMetadataValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255)
            .WithErrorCode(FormManagementErrors.TemplateNameRequired)
            .WithMessage(FormManagementErrors.MsgTemplateNameRequired);
    }
}

public sealed class UpdateTemplateContentValidator : AbstractValidator<UpdateTemplateContentRequest>
{
    public UpdateTemplateContentValidator()
    {
        RuleFor(x => x.DocxBase64).NotEmpty()
            .WithErrorCode(FormManagementErrors.TemplateContentRequired)
            .WithMessage(FormManagementErrors.MsgTemplateContentRequired);
    }
}

public sealed class CreateSubmissionValidator : AbstractValidator<CreateSubmissionRequest>
{
    public CreateSubmissionValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty()
            .WithErrorCode(FormManagementErrors.TemplateNotFound)
            .WithMessage(FormManagementErrors.MsgTemplateNotFound);
        RuleFor(x => x.Data).NotNull().Must(d => d.Count > 0)
            .WithErrorCode(FormManagementErrors.SubmissionDataRequired)
            .WithMessage(FormManagementErrors.MsgSubmissionDataRequired);
        RuleFor(x => x.ExportFormat).IsInEnum();
    }
}
