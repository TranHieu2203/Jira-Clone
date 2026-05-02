using BB.Common;

namespace Comment.Application;

public interface ICommentService
{
    Task<Result<PagedList<CommentDto>>> ListByIssueAsync(Guid issueId, int pageIndex = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Result<CommentDto>> CreateAsync(CreateCommentRequest request, CancellationToken ct = default);
    Task<Result<CommentDto>> UpdateAsync(Guid id, UpdateCommentRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
