using BB.Common;

namespace IssueLink.Application;

public interface IIssueLinkService
{
    /// <summary>Lấy outgoing + incoming link của 1 issue.</summary>
    Task<Result<IssueLinksForIssueDto>> ListByIssueAsync(Guid issueId, CancellationToken ct = default);

    /// <summary>Tạo link mới giữa 2 issue (current user là creator).</summary>
    Task<Result<IssueLinkDto>> CreateAsync(CreateIssueLinkRequest request, CancellationToken ct = default);

    /// <summary>Xoá link theo id (chỉ user là member của project source mới xoá được).</summary>
    Task<Result> DeleteAsync(Guid linkId, CancellationToken ct = default);
}
