namespace Comment.Application;

internal static class Mappers
{
    public static CommentDto ToDto(Domain.Comment c) =>
        new(c.Id, c.IssueId, c.AuthorId, c.Body, c.Mentions.ToList(), c.IsEdited, c.CreatedAt, c.UpdatedAt);
}
