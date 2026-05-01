namespace CustomField.Domain;

public static class CustomFieldErrors
{
    public const string NameRequired = "FIELD_NAME_REQUIRED";
    public const string KeyRequired = "FIELD_KEY_REQUIRED";
    public const string KeyInvalid = "FIELD_KEY_INVALID";
    public const string KeyDuplicated = "FIELD_KEY_DUPLICATED";
    public const string CannotDeleteSystem = "FIELD_SYSTEM_CANNOT_DELETE";
    public const string OptionValueRequired = "FIELD_OPTION_VALUE_REQUIRED";
    public const string OptionDuplicated = "FIELD_OPTION_DUPLICATED";
    public const string OptionNotFound = "FIELD_OPTION_NOT_FOUND";
    public const string OptionNotForType = "FIELD_OPTION_NOT_FOR_TYPE";
    public const string ContextNotFound = "FIELD_CONTEXT_NOT_FOUND";
    public const string ValueInvalid = "FIELD_VALUE_INVALID";
    public const string ValueRequired = "FIELD_VALUE_REQUIRED";
    public const string TypeHandlerMissing = "FIELD_TYPE_HANDLER_MISSING";

    public const string MsgNameRequired = "field.name.required";
    public const string MsgKeyRequired = "field.key.required";
    public const string MsgKeyInvalid = "field.key.invalid";
    public const string MsgKeyDup = "field.key.duplicated";
    public const string MsgCannotDeleteSystem = "field.system_cannot_delete";
    public const string MsgOptionValueRequired = "field.option.value_required";
    public const string MsgOptionDup = "field.option.duplicated";
    public const string MsgOptionNotFound = "field.option.not_found";
    public const string MsgOptionNotForType = "field.option.not_for_type";
    public const string MsgContextNotFound = "field.context.not_found";
    public const string MsgValueInvalid = "field.value.invalid";
    public const string MsgValueRequired = "field.value.required";
    public const string MsgTypeHandlerMissing = "field.type_handler_missing";
}
