using Project.Application.Repositories;
using Project.Domain;

namespace Project.Application;

public sealed class IssueTypeReader : IIssueTypeReader
{
    private readonly IProjectRepository _repo;

    public IssueTypeReader(IProjectRepository repo) => _repo = repo;

    public async Task<IssueTypeDto?> GetAsync(Guid issueTypeId, CancellationToken ct = default)
    {
        IssueType? t = await _repo.GetIssueTypeByIdAsync(issueTypeId, ct);
        return t is null ? null : Mappers.ToDto(t);
    }

    public async Task<IReadOnlyList<IssueTypeDto>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(projectId, ct);
        if (p is null) return Array.Empty<IssueTypeDto>();
        return p.IssueTypes.OrderBy(t => t.Order).Select(Mappers.ToDto).ToList();
    }

    public async Task<bool> ExistsInProjectAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default)
    {
        var p = await _repo.GetWithDetailsAsync(projectId, ct);
        return p is not null && p.IssueTypes.Any(t => t.Id == issueTypeId);
    }
}
