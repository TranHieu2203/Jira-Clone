using BB.Common;

namespace IssueLink.Domain.Events;

public sealed record IssueLinkAdded(Guid LinkId, Guid SourceIssueId, Guid TargetIssueId, IssueLinkType LinkType, Guid CreatedByUserId) : DomainEvent;
public sealed record IssueLinkRemoved(Guid LinkId, Guid SourceIssueId, Guid TargetIssueId, IssueLinkType LinkType, Guid RemovedByUserId) : DomainEvent;
