using BB.Common;

namespace Notification.Domain;

public sealed class InAppNotification : AuditableEntity
{
    public Guid RecipientUserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public bool IsRead { get; private set; }

    private InAppNotification() { }

    public InAppNotification(Guid recipientUserId, string type, string payloadJson)
    {
        RecipientUserId = recipientUserId;
        Type = type;
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson;
    }

    public void MarkRead() => IsRead = true;
}
