using System.Text.Json;

namespace CustomField.Application;

public sealed record CustomFieldDto(
    Guid Id, string Key, string Name, string? Description, int Type,
    bool IsSystem, bool IsSearchable, string ConfigJson,
    IReadOnlyList<CustomFieldOptionDto> Options,
    IReadOnlyList<CustomFieldContextDto> Contexts,
    DateTimeOffset CreatedAt);

public sealed record CustomFieldOptionDto(Guid Id, Guid? ParentOptionId, string Value, string Label, int Order, bool IsDisabled);

public sealed record CustomFieldContextDto(
    Guid Id, string Name, bool IsGlobal, bool IsRequired, string? DefaultValueJson,
    IReadOnlyList<Guid> ProjectIds, IReadOnlyList<Guid> IssueTypeIds);

public sealed record CreateCustomFieldRequest(string Key, string Name, int Type, string? Description, bool IsSearchable, string? ConfigJson);
public sealed record UpdateCustomFieldRequest(string Name, string? Description, bool IsSearchable, string? ConfigJson);

public sealed record AddOptionRequest(string Value, string Label, Guid? ParentOptionId, int? Order);
public sealed record UpdateOptionRequest(string Value, string Label, int Order);

public sealed record AddContextRequest(string Name, bool IsGlobal, bool IsRequired, string? DefaultValueJson, List<Guid>? ProjectIds, List<Guid>? IssueTypeIds);

public sealed record IssueFieldValueDto(Guid CustomFieldId, string FieldKey, int Type, JsonElement Value);

public sealed record SetIssueFieldValueRequest(Guid CustomFieldId, JsonElement Value);
public sealed record SetIssueFieldValuesRequest(Guid IssueId, Guid ProjectId, Guid IssueTypeId, List<SetIssueFieldValueRequest> Values);
