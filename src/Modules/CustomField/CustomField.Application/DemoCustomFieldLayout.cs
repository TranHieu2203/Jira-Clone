namespace CustomField.Application;

/// <summary>
/// Field demo + thứ tự “screen” (layout) khi gắn context cho từng project mới.
/// Key phải khớp seed trong module Infrastructure (CustomFieldSeeder).
/// </summary>
public static class DemoCustomFieldLayout
{
    public static readonly (string Key, int DisplayOrder)[] FieldEntries =
    [
        ("acceptance_criteria", 10),
        ("risk_level", 20),
        ("cf_story_points", 30),
        ("cf_target_date", 40),
        ("cf_components", 50),
        ("mandays", 60),
    ];
}
