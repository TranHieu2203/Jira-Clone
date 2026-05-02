using BB.Common;

namespace Attachment.Domain;

public sealed class IssueAttachment : AuditableEntity
{
    public Guid IssueId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;

    private IssueAttachment()
    {
    }

    public IssueAttachment(
        Guid issueId,
        Guid uploadedByUserId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey)
    {
        IssueId = issueId;
        UploadedByUserId = uploadedByUserId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
    }
}
