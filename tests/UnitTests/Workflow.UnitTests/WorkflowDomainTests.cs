using BB.Common;
using FluentAssertions;
using Workflow.Domain;

namespace Workflow.UnitTests;

public class WorkflowDomainTests
{
    [Fact]
    public void CreateForProject_WithValidInput_Succeeds()
    {
        var w = Domain.Workflow.CreateForProject(Guid.NewGuid(), "Software Simple", "SOFTWARE_SIMPLE");

        w.Name.Should().Be("Software Simple");
        w.Key.Should().Be("SOFTWARE_SIMPLE");
        w.IsTemplate.Should().BeFalse();
        w.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithInvalidKey_Throws()
    {
        var act = () => Domain.Workflow.CreateTemplate("X", "lowercase-key");
        act.Should().Throw<DomainException>().Where(ex => ex.Code == WorkflowErrors.KeyInvalid);
    }

    [Fact]
    public void AddStatus_FirstStatus_BecomesInitial()
    {
        var w = Domain.Workflow.CreateTemplate("Test", "TEST_WF");
        var todo = w.AddStatus("To Do", "TODO", StatusCategory.ToDo);

        w.InitialStatusId.Should().Be(todo.Id);
    }

    [Fact]
    public void AddStatus_DuplicateKey_Throws()
    {
        var w = Domain.Workflow.CreateTemplate("Test", "TEST_WF");
        w.AddStatus("To Do", "TODO", StatusCategory.ToDo);

        var act = () => w.AddStatus("Another", "todo", StatusCategory.ToDo);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == WorkflowErrors.StatusKeyDuplicated);
    }

    [Fact]
    public void AddTransition_DuplicateFromTo_Throws()
    {
        var w = Domain.Workflow.CreateTemplate("Test", "TEST_WF");
        var a = w.AddStatus("A", "A", StatusCategory.ToDo);
        var b = w.AddStatus("B", "B", StatusCategory.Done);

        w.AddTransition(a.Id, b.Id, "Go");
        var act = () => w.AddTransition(a.Id, b.Id, "Go again");

        act.Should().Throw<DomainException>().Where(ex => ex.Code == WorkflowErrors.TransitionDuplicated);
    }

    [Fact]
    public void RemoveStatus_WhenInUseByTransition_Throws()
    {
        var w = Domain.Workflow.CreateTemplate("Test", "TEST_WF");
        var a = w.AddStatus("A", "A", StatusCategory.ToDo);
        var b = w.AddStatus("B", "B", StatusCategory.Done);
        w.AddTransition(a.Id, b.Id, "Go");

        var act = () => w.RemoveStatus(b.Id);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == WorkflowErrors.StatusInUse);
    }

    [Fact]
    public void Transition_FromNullStatus_AppliesFromAnyStatus()
    {
        var w = Domain.Workflow.CreateTemplate("Test", "TEST_WF");
        var a = w.AddStatus("A", "A", StatusCategory.ToDo);
        var done = w.AddStatus("D", "D", StatusCategory.Done);
        var global = w.AddTransition(fromStatusId: null, toStatusId: done.Id, "Force Close");

        global.IsGlobal.Should().BeTrue();
        global.AppliesFrom(a.Id).Should().BeTrue();
        global.AppliesFrom(done.Id).Should().BeTrue();
    }
}
