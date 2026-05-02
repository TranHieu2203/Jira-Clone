using BB.Common;

namespace Comment.Domain.Events;

public sealed record CommentAdded(Guid CommentId, Guid IssueId, Guid AuthorId, string Body) : DomainEvent;
public sealed record CommentEdited(Guid CommentId, Guid IssueId, Guid AuthorId) : DomainEvent;
public sealed record CommentDeleted(Guid CommentId, Guid IssueId, Guid AuthorId) : DomainEvent;
