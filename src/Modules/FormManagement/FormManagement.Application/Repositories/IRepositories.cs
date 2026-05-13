using BB.Persistence;
using FormManagement.Domain;

namespace FormManagement.Application.Repositories;

public interface IMetadataRepository : IRepository<MetadataDef>
{
    Task<MetadataDef?> GetByValueAsync(string value, CancellationToken ct = default);
    Task<bool> ValueExistsAsync(string value, Guid? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<MetadataDef>> SearchAsync(string? keyword, string? group, CancellationToken ct = default);
}

public interface ITemplateRepository : IRepository<DocumentTemplate>
{
    Task<DocumentTemplate?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentTemplate>> SearchAsync(string? keyword, TemplateStatus? status, string? category, CancellationToken ct = default);
}

public interface ISubmissionRepository : IRepository<Submission>
{
    Task<IReadOnlyList<Submission>> ListByTemplateAsync(Guid templateId, int take = 50, CancellationToken ct = default);
}

public interface IFormManagementUnitOfWork : IUnitOfWork { }
