namespace Project.Application;

/// <summary>
/// Cross-module contract: cho phép Workflow / CustomField / Issue module
/// resolve thông tin IssueType mà không phụ thuộc Infrastructure của Project module.
/// </summary>
public interface IIssueTypeReader
{
    Task<IssueTypeDto?> GetAsync(Guid issueTypeId, CancellationToken ct = default);
    Task<IReadOnlyList<IssueTypeDto>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<bool> ExistsInProjectAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default);
}
