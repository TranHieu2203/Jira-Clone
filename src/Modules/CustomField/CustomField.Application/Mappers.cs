using CustomField.Domain;

namespace CustomField.Application;

internal static class Mappers
{
    public static CustomFieldDto ToDto(Domain.CustomField f) => new(
        f.Id, f.Key, f.Name, f.Description, (int)f.Type, f.IsSystem, f.IsSearchable, f.ConfigJson,
        f.Options.OrderBy(o => o.Order).Select(ToDto).ToList(),
        f.Contexts.Select(ToDto).ToList(),
        f.CreatedAt);

    public static CustomFieldOptionDto ToDto(CustomFieldOption o) =>
        new(o.Id, o.ParentOptionId, o.Value, o.Label, o.Order, o.IsDisabled);

    public static CustomFieldContextDto ToDto(CustomFieldContext c) =>
        new(c.Id, c.Name, c.IsGlobal, c.IsRequired, c.DefaultValueJson,
            c.ProjectIds.ToList(), c.IssueTypeIds.ToList());
}
