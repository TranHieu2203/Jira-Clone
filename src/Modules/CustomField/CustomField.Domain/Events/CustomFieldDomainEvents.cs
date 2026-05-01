using BB.Common;

namespace CustomField.Domain.Events;

public sealed record CustomFieldCreated(Guid FieldId, string Key, CustomFieldType Type) : DomainEvent;
public sealed record IssueFieldValueChanged(Guid IssueId, Guid FieldId, string ValueJson) : DomainEvent;
