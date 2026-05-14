namespace FormManagement.Domain;

public static class FormManagementErrors
{
    // Metadata
    public const string MetadataValueRequired = "METADATA_VALUE_REQUIRED";
    public const string MetadataValueInvalid = "METADATA_VALUE_INVALID";
    public const string MetadataValueDuplicated = "METADATA_VALUE_DUPLICATED";
    public const string MetadataLabelRequired = "METADATA_LABEL_REQUIRED";
    public const string MetadataTypeInvalid = "METADATA_TYPE_INVALID";
    public const string MetadataNotFound = "METADATA_NOT_FOUND";

    // Template
    public const string TemplateCodeRequired = "TEMPLATE_CODE_REQUIRED";
    public const string TemplateCodeInvalid = "TEMPLATE_CODE_INVALID";
    public const string TemplateCodeDuplicated = "TEMPLATE_CODE_DUPLICATED";
    public const string TemplateNameRequired = "TEMPLATE_NAME_REQUIRED";
    public const string TemplateContentRequired = "TEMPLATE_CONTENT_REQUIRED";
    public const string TemplateNotFound = "TEMPLATE_NOT_FOUND";
    public const string TemplateStatusInvalid = "TEMPLATE_STATUS_INVALID";

    // Submission
    public const string SubmissionDataRequired = "SUBMISSION_DATA_REQUIRED";
    public const string SubmissionNotFound = "SUBMISSION_NOT_FOUND";

    // Conversion
    public const string ConversionUnsupported = "CONVERSION_UNSUPPORTED";
    public const string ConversionFailed = "CONVERSION_FAILED";

    // i18n message keys (FE translate)
    public const string MsgMetadataValueRequired = "form_mgmt.metadata.value.required";
    public const string MsgMetadataValueInvalid = "form_mgmt.metadata.value.invalid";
    public const string MsgMetadataValueDup = "form_mgmt.metadata.value.duplicated";
    public const string MsgMetadataLabelRequired = "form_mgmt.metadata.label.required";
    public const string MsgMetadataTypeInvalid = "form_mgmt.metadata.type.invalid";
    public const string MsgMetadataNotFound = "form_mgmt.metadata.not_found";

    public const string MsgTemplateCodeRequired = "form_mgmt.template.code.required";
    public const string MsgTemplateCodeInvalid = "form_mgmt.template.code.invalid";
    public const string MsgTemplateCodeDup = "form_mgmt.template.code.duplicated";
    public const string MsgTemplateNameRequired = "form_mgmt.template.name.required";
    public const string MsgTemplateContentRequired = "form_mgmt.template.content.required";
    public const string MsgTemplateNotFound = "form_mgmt.template.not_found";
    public const string MsgTemplateStatusInvalid = "form_mgmt.template.status.invalid";

    public const string MsgSubmissionDataRequired = "form_mgmt.submission.data.required";
    public const string MsgSubmissionNotFound = "form_mgmt.submission.not_found";

    public const string MsgConversionUnsupported = "form_mgmt.conversion.unsupported";
    public const string MsgConversionFailed = "form_mgmt.conversion.failed";
}

/// <summary>Type của một metadata field — quyết định input control ở FE và validation ở BE/mail-merge.</summary>
public enum MetadataType
{
    Text = 1,
    Number = 2,
    Date = 3,
    Currency = 4,
    Textarea = 5
}

/// <summary>Trạng thái template — DRAFT đang soạn, PUBLISHED đã active cho user nhập data, ARCHIVED gác lại.</summary>
public enum TemplateStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

/// <summary>Format export khi mail-merge submission. DOCX là default; PDF qua OnlyOffice DocServer.</summary>
public enum ExportFormat
{
    Docx = 2,
    Pdf = 3
}
