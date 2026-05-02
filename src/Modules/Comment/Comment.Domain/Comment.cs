using System.Text.RegularExpressions;
using BB.Common;
using Comment.Domain.Events;

namespace Comment.Domain;

public sealed class Comment : AggregateRoot, ISoftDeletable
{
    public const int BodyMaxLength = 10_000;

    /// <summary>Bắt mention dạng <c>@username</c>. Username regex tối thiểu — không cho space.</summary>
    private static readonly Regex MentionPattern = new(@"@([a-zA-Z0-9_.\-]{1,64})", RegexOptions.Compiled);

    public Guid IssueId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsEdited { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<string> _mentions = new();
    public IReadOnlyList<string> Mentions => _mentions;

    private Comment() { }

    public Comment(Guid issueId, Guid authorId, string body)
    {
        EnsureBody(body);

        IssueId = issueId;
        AuthorId = authorId;
        Body = body.Trim();
        ExtractMentions();

        RaiseDomainEvent(new CommentAdded(Id, issueId, authorId, Body));
    }

    private static void EnsureBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException(CommentErrors.BodyRequired, CommentErrors.MsgBodyRequired);
        if (body.Length > BodyMaxLength)
            throw new DomainException(CommentErrors.BodyTooLong, CommentErrors.MsgBodyTooLong);
    }

    private void ExtractMentions()
    {
        _mentions.Clear();
        foreach (Match m in MentionPattern.Matches(Body))
        {
            var username = m.Groups[1].Value;
            if (!_mentions.Contains(username, StringComparer.OrdinalIgnoreCase))
                _mentions.Add(username);
        }
    }

    public void Edit(Guid editorId, string body)
    {
        if (editorId != AuthorId)
            throw new DomainException(CommentErrors.NotAuthor, CommentErrors.MsgNotAuthor);
        EnsureBody(body);
        if (Body == body.Trim()) return;

        Body = body.Trim();
        IsEdited = true;
        ExtractMentions();
        RaiseDomainEvent(new CommentEdited(Id, IssueId, AuthorId));
    }

    public void EnsureCanDelete(Guid actorId)
    {
        if (actorId != AuthorId)
            throw new DomainException(CommentErrors.NotAuthor, CommentErrors.MsgNotAuthor);
    }

    public void RaiseDeletedEvent(Guid actorId)
    {
        RaiseDomainEvent(new CommentDeleted(Id, IssueId, actorId));
    }
}
