using AuditLog.Application.Repositories;
using BB.Common;

namespace AuditLog.Application;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly IAuditEntryRepository _repo;

    public AuditQueryService(IAuditEntryRepository repo) => _repo = repo;

    public async Task<Result<PagedList<AuditEntryDto>>> SearchAsync(SearchAuditRequest request, CancellationToken ct = default)
    {
        var criteria = new SearchAuditCriteria(
            request.ActorUserId,
            string.IsNullOrWhiteSpace(request.Action) ? null : request.Action.Trim(),
            string.IsNullOrWhiteSpace(request.Scope) ? null : request.Scope.Trim(),
            request.ScopeId,
            request.From,
            request.To,
            Math.Max(request.PageIndex, 1),
            Math.Clamp(request.PageSize, 1, 200));

        PagedList<Domain.AuditEntry> page = await _repo.SearchAsync(criteria, ct);
        var items = page.Items.Select(ToDto).ToList();
        return Result.Success(new PagedList<AuditEntryDto>(items, page.TotalCount, page.PageIndex, page.PageSize));
    }

    private static AuditEntryDto ToDto(Domain.AuditEntry e) =>
        new(e.Id, e.ActorUserId, e.Action, e.Scope, e.ScopeId, e.PayloadJson, e.OccurredAt, e.TraceId);
}
