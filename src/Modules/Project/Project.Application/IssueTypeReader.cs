using Project.Application.Repositories;

namespace Project.Application;

public sealed class IssueTypeReader : IIssueTypeReader
{
    private readonly IProjectRepository _repo;

    public IssueTypeReader(IProjectRepository repo) => _repo = repo;

    public async Task<IssueTypeDto?> GetAsync(Guid issueTypeId, CancellationToken ct = default)
    {
        // Hơi tốn vì phải scan; với MVP chấp nhận. Sau này nếu nóng — thêm read-model riêng.
        // Tạm: để Issue module truyền projectId kèm theo, nên không gọi method này nhiều.
        return await Task.FromResult<IssueTypeDto?>(null);
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
