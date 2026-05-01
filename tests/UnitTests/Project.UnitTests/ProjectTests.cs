using BB.Common;
using FluentAssertions;
using Project.Domain;
using Project.Domain.Events;

namespace Project.UnitTests;

public class ProjectTests
{
    [Fact]
    public void Constructor_SeedsFiveSystemIssueTypes()
    {
        var p = new Domain.Project(Guid.NewGuid(), "Acme Web", "ACME", Guid.NewGuid(), ProjectType.Scrum);

        p.IssueTypes.Should().HaveCount(5);
        p.IssueTypes.Select(t => t.Key).Should().BeEquivalentTo("EPIC", "STORY", "TASK", "BUG", "SUBTASK");
        p.IssueTypes.Should().OnlyContain(t => t.IsSystem);
        p.IssueTypes.Single(t => t.Key == "SUBTASK").IsSubtask.Should().BeTrue();
    }

    [Fact]
    public void Constructor_LeadIsAdmin()
    {
        var lead = Guid.NewGuid();
        var p = new Domain.Project(Guid.NewGuid(), "X", "PRJ", lead, ProjectType.Kanban);

        p.Members.Should().ContainSingle(m => m.UserId == lead && m.Role == ProjectRole.Admin);
    }

    [Theory]
    [InlineData("a")]         // too short
    [InlineData("acme")]      // lowercase
    [InlineData("AC-ME")]     // hyphen
    [InlineData("ACMETEAM_X")]// underscore (key only allows A-Z 0-9)
    public void Constructor_InvalidKey_Throws(string key)
    {
        var act = () => new Domain.Project(Guid.NewGuid(), "X", key, Guid.NewGuid(), ProjectType.Scrum);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.ProjectKeyInvalid);
    }

    [Fact]
    public void RemoveIssueType_System_Throws()
    {
        var p = new Domain.Project(Guid.NewGuid(), "X", "PRJ", Guid.NewGuid(), ProjectType.Scrum);
        var bug = p.IssueTypes.Single(t => t.Key == "BUG");

        var act = () => p.RemoveIssueType(bug.Id);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.IssueTypeIsSystemCannotDelete);
    }

    [Fact]
    public void AddIssueType_DuplicateKey_Throws()
    {
        var p = new Domain.Project(Guid.NewGuid(), "X", "PRJ", Guid.NewGuid(), ProjectType.Scrum);

        var act = () => p.AddIssueType("Another Bug", "BUG", null, null, false);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.IssueTypeKeyDuplicated);
    }

    [Fact]
    public void AllocateIssueNumber_IsSequential()
    {
        var p = new Domain.Project(Guid.NewGuid(), "X", "PRJ", Guid.NewGuid(), ProjectType.Scrum);

        var n1 = p.AllocateIssueNumber();
        var n2 = p.AllocateIssueNumber();
        var n3 = p.AllocateIssueNumber();

        n1.Should().Be(1);
        n2.Should().Be(2);
        n3.Should().Be(3);
        p.NextIssueNumber.Should().Be(4);
    }

    [Fact]
    public void RemoveMember_Lead_Throws()
    {
        var lead = Guid.NewGuid();
        var p = new Domain.Project(Guid.NewGuid(), "X", "PRJ", lead, ProjectType.Scrum);

        var act = () => p.RemoveMember(lead);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.ProjectCannotRemoveLead);
    }

    [Fact]
    public void Created_RaisesProjectCreatedEvent()
    {
        var p = new Domain.Project(Guid.NewGuid(), "X", "PRJ", Guid.NewGuid(), ProjectType.Scrum);

        p.DomainEvents.Should().ContainSingle(e => e is ProjectCreated);
    }
}
