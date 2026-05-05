using IssueLink.Domain;

namespace IssueLink.Application;

/// <summary>1 row link với hướng (forward) — caller biết source vs target.</summary>
public sealed record IssueLinkDto(
    Guid Id,
    Guid SourceIssueId,
    Guid TargetIssueId,
    IssueLinkType LinkType,
    string LinkTypeKey,
    DateTimeOffset CreatedAt);

/// <summary>
/// Hai bucket cho FE issue detail:
/// <list type="bullet">
///   <item><c>Outgoing</c>: link mà current issue là <b>source</b> — UI hiển thị forward label ("blocks", "duplicates"…)</item>
///   <item><c>Incoming</c>: link mà current issue là <b>target</b> — UI hiển thị inverse label ("blocked by", "duplicated by"…)</item>
/// </list>
/// Issue keys + summaries được resolve bởi caller (Issue module) — bucket này chỉ trả id để giảm coupling.
/// </summary>
public sealed record IssueLinksForIssueDto(
    Guid IssueId,
    IReadOnlyList<IssueLinkDto> Outgoing,
    IReadOnlyList<IssueLinkDto> Incoming);

public sealed record CreateIssueLinkRequest(
    Guid SourceIssueId,
    Guid TargetIssueId,
    IssueLinkType LinkType);
