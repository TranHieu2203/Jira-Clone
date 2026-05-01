using BB.Common;
using FluentAssertions;
using Issue.Domain;
using Issue.Domain.Events;

namespace Issue.UnitTests;

public class IssueDomainTests
{
    private static Domain.Issue NewIssue(Guid? reporterId = null, Guid? assigneeId = null)
    {
        return new Domain.Issue(
            projectId: Guid.NewGuid(),
            key: "PRJ-1",
            number: 1,
            issueTypeId: Guid.NewGuid(),
            workflowId: Guid.NewGuid(),
            initialStatusId: Guid.NewGuid(),
            summary: "Initial summary",
            reporterId: reporterId ?? Guid.NewGuid(),
            assigneeId: assigneeId);
    }

    [Fact]
    public void Constructor_AddsReporterAsWatcher()
    {
        var reporter = Guid.NewGuid();
        var i = NewIssue(reporterId: reporter);

        i.IsWatching(reporter).Should().BeTrue();
        i.Watchers.Should().ContainSingle();
    }

    [Fact]
    public void Constructor_RaisesIssueCreatedEvent()
    {
        var i = NewIssue();
        i.DomainEvents.Should().ContainSingle(e => e is IssueCreated);
    }

    [Fact]
    public void Constructor_EmptySummary_Throws()
    {
        var act = () => new Domain.Issue(
            Guid.NewGuid(), "PRJ-1", 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "  ", Guid.NewGuid());
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.SummaryRequired);
    }

    [Fact]
    public void Constructor_TooLongSummary_Throws()
    {
        var longSummary = new string('x', Domain.Issue.SummaryMaxLength + 1);
        var act = () => new Domain.Issue(
            Guid.NewGuid(), "PRJ-1", 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            longSummary, Guid.NewGuid());
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.SummaryTooLong);
    }

    [Fact]
    public void Assign_NewAssignee_AutoAddsWatcher()
    {
        var i = NewIssue();
        var assignee = Guid.NewGuid();

        i.Assign(assignee);

        i.AssigneeId.Should().Be(assignee);
        i.IsWatching(assignee).Should().BeTrue();
        i.DomainEvents.OfType<IssueAssigneeChanged>().Should().HaveCount(1);
        i.DomainEvents.OfType<IssueWatcherAdded>().Should().HaveCount(1);
    }

    [Fact]
    public void Assign_SameAssignee_NoEvent()
    {
        var assignee = Guid.NewGuid();
        var i = NewIssue(assigneeId: assignee);
        i.ClearDomainEvents();

        i.Assign(assignee);

        i.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetParent_Self_Throws()
    {
        var i = NewIssue();
        var act = () => i.SetParent(i.Id);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.ParentSelf);
    }

    [Fact]
    public void TransitionTo_NewStatus_RaisesEvent()
    {
        var i = NewIssue();
        i.ClearDomainEvents();
        var newStatus = Guid.NewGuid();
        var transition = Guid.NewGuid();

        i.TransitionTo(newStatus, transition);

        i.CurrentStatusId.Should().Be(newStatus);
        i.DomainEvents.OfType<IssueStatusChanged>().Should().ContainSingle();
    }

    [Fact]
    public void TransitionTo_SameStatus_NoEvent()
    {
        var i = NewIssue();
        i.ClearDomainEvents();
        i.TransitionTo(i.CurrentStatusId, Guid.NewGuid());
        i.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddWatcher_Duplicate_Throws()
    {
        var i = NewIssue();
        var act = () => i.AddWatcher(i.ReporterId);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.WatcherDuplicated);
    }

    [Fact]
    public void RemoveWatcher_NotFound_Throws()
    {
        var i = NewIssue();
        var act = () => i.RemoveWatcher(Guid.NewGuid());
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.WatcherNotFound);
    }

    [Fact]
    public void SetLabels_Deduplicates_CaseInsensitive()
    {
        var i = NewIssue();
        i.SetLabels(new[] { "bug", "BUG", "feature", "Feature" });
        i.Labels.Count.Should().Be(2);
    }

    [Fact]
    public void SetTimeTracking_Negative_Throws()
    {
        var i = NewIssue();
        var act = () => i.SetTimeTracking(-1, null, null);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.EstimateNegative);
    }

    [Fact]
    public void Archive_Twice_Throws()
    {
        var i = NewIssue();
        i.Archive();
        var act = () => i.Archive();
        act.Should().Throw<DomainException>().Where(ex => ex.Code == IssueErrors.AlreadyArchived);
    }

    [Fact]
    public void ChangePriority_Different_RaisesEvent()
    {
        var i = NewIssue();
        i.ClearDomainEvents();
        i.ChangePriority(Priority.High);
        i.DomainEvents.OfType<IssueUpdated>().Should().ContainSingle(u => u.FieldName == "Priority");
    }
}
