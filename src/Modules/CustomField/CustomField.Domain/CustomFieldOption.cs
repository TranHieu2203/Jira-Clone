using BB.Common;

namespace CustomField.Domain;

public sealed class CustomFieldOption : BaseEntity
{
    public Guid CustomFieldId { get; private set; }
    public Guid? ParentOptionId { get; private set; }      // cascading
    public string Value { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsDisabled { get; private set; }

    private CustomFieldOption() { }

    internal CustomFieldOption(Guid customFieldId, string value, string label, int order, Guid? parentOptionId = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(CustomFieldErrors.OptionValueRequired, CustomFieldErrors.MsgOptionValueRequired);

        CustomFieldId = customFieldId;
        Value = value.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? value.Trim() : label.Trim();
        Order = order;
        ParentOptionId = parentOptionId;
    }

    internal void Update(string value, string label, int order)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(CustomFieldErrors.OptionValueRequired, CustomFieldErrors.MsgOptionValueRequired);
        Value = value.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? value.Trim() : label.Trim();
        Order = order;
    }

    internal void Disable() => IsDisabled = true;
    internal void Enable() => IsDisabled = false;
}
