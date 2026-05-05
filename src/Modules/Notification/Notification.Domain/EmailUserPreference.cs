using BB.Common;

namespace Notification.Domain;

/// <summary>
/// User opt-out preferences cho email notification. Default = nhận tất cả (tất cả flag = false).
///
/// Mapping templateKey → flag:
/// - <c>issue.assignee_changed</c> → <see cref="NoAssignee"/>
/// - <c>issue.status_changed</c> → <see cref="NoStatus"/>
/// - <c>comment.added</c> → <see cref="NoComment"/>
/// (Mention hiện gộp chung template comment.added; thêm flag riêng nếu tách template sau.)
/// </summary>
public sealed class EmailUserPreference : AuditableEntity
{
    public Guid UserId { get; private set; }
    public bool NoAssignee { get; private set; }
    public bool NoStatus { get; private set; }
    public bool NoComment { get; private set; }
    public bool NoMention { get; private set; }

    private EmailUserPreference() { }

    public EmailUserPreference(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("userId required", nameof(userId));
        UserId = userId;
    }

    public void Update(bool noAssignee, bool noStatus, bool noComment, bool noMention)
    {
        NoAssignee = noAssignee;
        NoStatus = noStatus;
        NoComment = noComment;
        NoMention = noMention;
    }

    /// <summary>Trả true nếu user đã opt-out cho templateKey này.</summary>
    public bool IsOptedOut(string templateKey) => templateKey switch
    {
        "issue.assignee_changed" => NoAssignee,
        "issue.status_changed" => NoStatus,
        "comment.added" => NoComment,
        _ => false
    };
}
