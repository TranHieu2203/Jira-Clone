namespace Comment.Domain;

public static class CommentErrors
{
    public const string BodyRequired = "COMMENT_BODY_REQUIRED";
    public const string BodyTooLong = "COMMENT_BODY_TOO_LONG";
    public const string NotAuthor = "COMMENT_NOT_AUTHOR";
    public const string NotFound = "COMMENT_NOT_FOUND";

    public const string MsgBodyRequired = "comment.body.required";
    public const string MsgBodyTooLong = "comment.body.too_long";
    public const string MsgNotAuthor = "comment.not_author";
    public const string MsgNotFound = "comment.not_found";
}
