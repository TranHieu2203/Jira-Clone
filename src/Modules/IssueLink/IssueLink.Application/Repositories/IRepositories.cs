using BB.Persistence;

namespace IssueLink.Application.Repositories;

public interface IIssueLinkRepository : IRepository<Domain.IssueLink>
{
    /// <summary>Link mà issue này là source.</summary>
    Task<IReadOnlyList<Domain.IssueLink>> ListBySourceAsync(Guid sourceIssueId, CancellationToken ct = default);

    /// <summary>Link mà issue này là target.</summary>
    Task<IReadOnlyList<Domain.IssueLink>> ListByTargetAsync(Guid targetIssueId, CancellationToken ct = default);

    /// <summary>Check duplicate trước khi insert (trả true nếu cặp source→target+type đã tồn tại).</summary>
    Task<bool> ExistsAsync(Guid sourceIssueId, Guid targetIssueId, Domain.IssueLinkType linkType, CancellationToken ct = default);
}

public interface IIssueLinkUnitOfWork : IUnitOfWork { }
