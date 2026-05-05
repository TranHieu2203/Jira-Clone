using BB.Common;
using IssueLink.Domain.Events;

namespace IssueLink.Domain;

/// <summary>
/// Quan hệ có hướng giữa 2 issue. Một row đại diện cho 1 chiều của quan hệ —
/// FE query incoming links của target để hiển thị inverse label.
///
/// Aggregate root: tự đóng gói invariants (cấm self-link, type bắt buộc hợp lệ).
/// Không soft-delete: link bị xoá là xoá cứng (không có business value để giữ history).
/// </summary>
public sealed class IssueLink : AggregateRoot
{
    public Guid SourceIssueId { get; private set; }
    public Guid SourceProjectId { get; private set; }
    public Guid TargetIssueId { get; private set; }
    public Guid TargetProjectId { get; private set; }
    public IssueLinkType LinkType { get; private set; }

    private IssueLink() { }

    public IssueLink(
        Guid sourceIssueId,
        Guid sourceProjectId,
        Guid targetIssueId,
        Guid targetProjectId,
        IssueLinkType linkType,
        Guid createdByUserId)
    {
        if (sourceIssueId == targetIssueId)
            throw new DomainException(IssueLinkErrors.SelfLink, IssueLinkErrors.MsgSelfLink);
        if (!Enum.IsDefined(typeof(IssueLinkType), linkType))
            throw new DomainException("ISSUE_LINK_TYPE_INVALID", "issue_link.type.invalid");

        SourceIssueId = sourceIssueId;
        SourceProjectId = sourceProjectId;
        TargetIssueId = targetIssueId;
        TargetProjectId = targetProjectId;
        LinkType = linkType;

        RaiseDomainEvent(new IssueLinkAdded(Id, sourceIssueId, targetIssueId, linkType, createdByUserId));
    }

    /// <summary>Raise event xoá — caller (service) phải gọi trước khi remove.</summary>
    public void RaiseRemovedEvent(Guid removedByUserId)
    {
        RaiseDomainEvent(new IssueLinkRemoved(Id, SourceIssueId, TargetIssueId, LinkType, removedByUserId));
    }
}
