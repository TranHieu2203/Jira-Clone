using BB.Persistence;
using CustomField.Domain;

namespace CustomField.Application.Repositories;

public interface ICustomFieldRepository : IRepository<Domain.CustomField>
{
    Task<Domain.CustomField?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Domain.CustomField?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.CustomField>> ListAllAsync(CancellationToken ct = default);
    Task<bool> KeyExistsAsync(string key, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Lấy các field có context áp dụng vào (project, issueType).</summary>
    Task<IReadOnlyList<Domain.CustomField>> ResolveForAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default);
}

public interface IIssueFieldValueRepository : IRepository<IssueFieldValue>
{
    Task<IReadOnlyList<IssueFieldValue>> ListByIssueAsync(Guid issueId, CancellationToken ct = default);
    Task<IssueFieldValue?> GetAsync(Guid issueId, Guid fieldId, CancellationToken ct = default);
    Task RemoveAllForIssueAsync(Guid issueId, CancellationToken ct = default);
}

public interface ICustomFieldUnitOfWork : IUnitOfWork { }
