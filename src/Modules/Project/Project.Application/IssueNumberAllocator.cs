using BB.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Application.Repositories;

namespace Project.Application;

/// <summary>
/// Cấp phát issue number atomic per project, có retry khi gặp concurrency conflict.
///
/// C5: hai user tạo issue đồng thời cho cùng project sẽ race trên `NextIssueNumber`.
/// `NextIssueNumber` đã được đánh dấu là concurrency token (xem <c>ProjectDbContext</c>),
/// nên EF sinh UPDATE với clause `WHERE id = ? AND next_issue_number = ?`.
/// Thread thua sẽ nhận `DbUpdateConcurrencyException` → ta reload state và thử lại.
/// </summary>
public sealed class IssueNumberAllocator : IIssueNumberAllocator
{
    /// <summary>Số lần thử tối đa trước khi bỏ cuộc — đủ cho ~10 caller cùng race.</summary>
    private const int MaxRetries = 10;

    private readonly IProjectRepository _repo;
    private readonly IProjectUnitOfWork _uow;
    private readonly ILogger<IssueNumberAllocator> _logger;

    public IssueNumberAllocator(IProjectRepository repo, IProjectUnitOfWork uow, ILogger<IssueNumberAllocator> logger)
    {
        _repo = repo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<AllocatedIssueNumber>> AllocateAsync(Guid projectId, CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            var project = await _repo.GetByIdAsync(projectId, ct);
            if (project is null)
                return Result.Failure<AllocatedIssueNumber>(ErrorType.NotFound, "project.not_found");

            var number = project.AllocateIssueNumber();
            _repo.Update(project);

            try
            {
                await _uow.SaveChangesAsync(ct);
                return Result.Success(new AllocatedIssueNumber(project.Key, number));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Project's NextIssueNumber đã bị thread khác cập nhật giữa lúc Read và Update.
                // Reload + retry. Detach entry hiện tại để vòng lặp tới đọc snapshot mới.
                _logger.LogDebug(
                    "Issue number race detected (attempt {Attempt}/{Max}) project={ProjectId}",
                    attempt, MaxRetries, projectId);
                _uow.DiscardChanges();
            }
        }

        _logger.LogWarning("Issue number allocation gave up after {Max} retries for project {ProjectId}", MaxRetries, projectId);
        return Result.Failure<AllocatedIssueNumber>(ErrorType.Conflict, "issue.allocate_number.conflict");
    }
}
