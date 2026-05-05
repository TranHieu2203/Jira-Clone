namespace IssueLink.Domain;

public static class IssueLinkErrors
{
    public const string SelfLink = "ISSUE_LINK_SELF";
    public const string MsgSelfLink = "issue_link.self_link";

    public const string DuplicateLink = "ISSUE_LINK_DUPLICATE";
    public const string MsgDuplicateLink = "issue_link.duplicate";

    public const string NotFound = "ISSUE_LINK_NOT_FOUND";
    public const string MsgNotFound = "issue_link.not_found";

    public const string SourceMissing = "ISSUE_LINK_SOURCE_MISSING";
    public const string MsgSourceMissing = "issue_link.source.missing";

    public const string TargetMissing = "ISSUE_LINK_TARGET_MISSING";
    public const string MsgTargetMissing = "issue_link.target.missing";
}
