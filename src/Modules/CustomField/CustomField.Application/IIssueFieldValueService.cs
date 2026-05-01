using BB.Common;

namespace CustomField.Application;

/// <summary>
/// Service đọc/ghi giá trị custom field của 1 issue. Issue module gọi vào đây
/// (qua Application interface) — không phụ thuộc Infrastructure.
/// </summary>
public interface IIssueFieldValueService
{
    Task<Result<IReadOnlyList<IssueFieldValueDto>>> ListByIssueAsync(Guid issueId, CancellationToken ct = default);

    /// <summary>
    /// Validate + persist giá trị field. Resolve field theo (project, issueType) qua
    /// CustomFieldContext. Required field thiếu → fail. Field không có context khớp → bỏ qua.
    /// </summary>
    Task<Result> SetValuesAsync(SetIssueFieldValuesRequest request, CancellationToken ct = default);

    Task<Result> ClearForIssueAsync(Guid issueId, CancellationToken ct = default);
}
