namespace CustomField.Application;

public sealed record IssueFieldIndexedCriterion(
    Guid CustomFieldId,
    string? IndexedStringEquals,
    decimal? IndexedNumberEquals,
    DateTimeOffset? IndexedDateEquals);

/// <summary>Lọc issue theo cột index trên <c>issue_field_values</c>.</summary>
public interface IIssueFieldValueIssueFilter
{
    /// <summary>null = không áp predicate CFV; empty = không khớp issue nào.</summary>
    Task<IReadOnlySet<Guid>?> MatchingIssueIdsAsync(IReadOnlyList<IssueFieldIndexedCriterion> criteria, CancellationToken ct = default);
}
