namespace Attachment.Application;

public sealed record AttachmentDto(
    Guid Id,
    Guid IssueId,
    Guid UploadedByUserId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public sealed record AttachmentDownload(Stream Stream, string ContentType, string FileName);
