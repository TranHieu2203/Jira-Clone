using BB.Common;
using Project.Application.Repositories;

namespace Project.Application;

public sealed class IssueNumberAllocator : IIssueNumberAllocator
{
    private readonly IProjectRepository _repo;
    private readonly IProjectUnitOfWork _uow;

    public IssueNumberAllocator(IProjectRepository repo, IProjectUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result<AllocatedIssueNumber>> AllocateAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null)
            return Result.Failure<AllocatedIssueNumber>(ErrorType.NotFound, "project.not_found");

        var number = project.AllocateIssueNumber();
        _repo.Update(project);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new AllocatedIssueNumber(project.Key, number));
    }
}
