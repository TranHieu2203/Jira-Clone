using FluentAssertions;
using Issue.Application.Repositories;
using Issue.Infrastructure;
using IssueAggregate = Issue.Domain.Issue;

namespace Issue.UnitTests;

public sealed class AccessibleProjectIdsSpecificationTests
{
    [Fact]
    public void AccessibleProjectIds_filters_by_project_membership()
    {
        Guid allowed = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid other = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var allowedSet = new HashSet<Guid> { allowed };

        var issueOk = new IssueAggregate(
            allowed, "K-1", 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Summary", Guid.NewGuid());
        var issueNo = new IssueAggregate(
            other, "K-2", 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Summary", Guid.NewGuid());

        var criteria = new IssueSearchCriteria(
            ProjectId: null,
            IssueTypeId: null,
            AssigneeId: null,
            ReporterId: null,
            CurrentStatusId: null,
            Priority: null,
            TextSearch: null,
            IncludeArchived: false,
            PageIndex: 1,
            PageSize: 50,
            Sort: null,
            AssigneeUnassignedOnly: false,
            RestrictToIssueIds: null,
            ExcludeIssueIds: null,
            CurrentStatusIds: null,
            AccessibleProjectIds: allowedSet);

        var spec = IssueSpecifications.From(criteria);
        var pred = spec.Criteria.Compile();

        pred(issueOk).Should().BeTrue();
        pred(issueNo).Should().BeFalse();
    }

    [Fact]
    public void AccessibleProjectIds_empty_set_matches_nothing()
    {
        Guid pid = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var issue = new IssueAggregate(
            pid, "K-3", 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Summary", Guid.NewGuid());

        var criteria = new IssueSearchCriteria(
            ProjectId: null,
            IssueTypeId: null,
            AssigneeId: null,
            ReporterId: null,
            CurrentStatusId: null,
            Priority: null,
            TextSearch: null,
            IncludeArchived: false,
            PageIndex: 1,
            PageSize: 50,
            Sort: null,
            AssigneeUnassignedOnly: false,
            RestrictToIssueIds: null,
            ExcludeIssueIds: null,
            CurrentStatusIds: null,
            AccessibleProjectIds: new HashSet<Guid>());

        var spec = IssueSpecifications.From(criteria);
        var pred = spec.Criteria.Compile();

        pred(issue).Should().BeFalse();
    }
}
