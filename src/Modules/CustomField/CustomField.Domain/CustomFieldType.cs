namespace CustomField.Domain;

public enum CustomFieldType
{
    Text = 1,
    TextArea = 2,
    Number = 3,
    Decimal = 4,
    Date = 5,
    DateTime = 6,

    Select = 10,
    MultiSelect = 11,
    Cascading = 12,

    User = 20,
    UserMulti = 21,

    Checkbox = 30,
    Url = 31,
    Label = 32,

    /// <summary>System-reserved fields như summary, description... không tạo qua API.</summary>
    SystemReserved = 99
}
