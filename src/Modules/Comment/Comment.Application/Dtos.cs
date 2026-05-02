namespace Comment.Application;

public sealed record CommentDto(
    Guid Id,
    Guid IssueId,
    Guid AuthorId,
    string Body,
    IReadOnlyList<string> Mentions,
    bool IsEdited,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateCommentRequest(Guid IssueId, string Body);
public sealed record UpdateCommentRequest(string Body);
