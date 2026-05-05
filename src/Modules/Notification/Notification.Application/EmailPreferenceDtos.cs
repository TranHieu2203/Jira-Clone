namespace Notification.Application;

public sealed record EmailUserPreferenceDto(
    Guid UserId,
    bool NoAssignee,
    bool NoStatus,
    bool NoComment,
    bool NoMention);

public sealed record UpdateEmailPreferenceRequest(
    bool NoAssignee,
    bool NoStatus,
    bool NoComment,
    bool NoMention);
