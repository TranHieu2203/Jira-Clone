using BB.Common;
using CustomField.Domain;
using FluentAssertions;

namespace CustomField.UnitTests;

public class CustomFieldDomainTests
{
    [Fact]
    public void Create_ValidInput_Succeeds()
    {
        var f = new Domain.CustomField("story_points", "Story Points", CustomFieldType.Number);
        f.Key.Should().Be("story_points");
        f.Type.Should().Be(CustomFieldType.Number);
        f.IsSearchable.Should().BeFalse();
    }

    [Theory]
    [InlineData("Bad")]                    // uppercase
    [InlineData("ab")]                      // too short
    [InlineData("9start")]                  // starts with digit
    [InlineData("with-dash")]               // dash
    public void Create_InvalidKey_Throws(string key)
    {
        var act = () => new Domain.CustomField(key, "x", CustomFieldType.Text);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == CustomFieldErrors.KeyInvalid);
    }

    [Fact]
    public void AddOption_OnNonOptionField_Throws()
    {
        var f = new Domain.CustomField("text_field", "X", CustomFieldType.Text);
        var act = () => f.AddOption("v1", "V1");
        act.Should().Throw<DomainException>().Where(ex => ex.Code == CustomFieldErrors.OptionNotForType);
    }

    [Fact]
    public void AddOption_DuplicateValue_Throws()
    {
        var f = new Domain.CustomField("priority_x", "X", CustomFieldType.Select);
        f.AddOption("HIGH", "High");
        var act = () => f.AddOption("high", "High again");
        act.Should().Throw<DomainException>().Where(ex => ex.Code == CustomFieldErrors.OptionDuplicated);
    }

    [Fact]
    public void ResolveContext_PrefersScopedOverGlobal()
    {
        var f = new Domain.CustomField("severity_x", "Severity", CustomFieldType.Select);
        var projectId = Guid.NewGuid();
        var issueTypeId = Guid.NewGuid();

        var globalCtx = f.AddContext("Global", isGlobal: true, isRequired: false, defaultValueJson: null);
        var scopedCtx = f.AddContext("Project-specific", isGlobal: false, isRequired: true, defaultValueJson: null,
            projectIds: new[] { projectId });

        var resolved = f.ResolveContext(projectId, issueTypeId);
        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be(scopedCtx.Id);
    }

    [Fact]
    public void ResolveContext_NoScopedMatch_FallsBackToGlobal()
    {
        var f = new Domain.CustomField("estimate_x", "X", CustomFieldType.Number);
        var globalCtx = f.AddContext("Global", isGlobal: true, isRequired: false, defaultValueJson: null);

        var resolved = f.ResolveContext(Guid.NewGuid(), Guid.NewGuid());
        resolved!.Id.Should().Be(globalCtx.Id);
    }

    [Fact]
    public void Context_AppliesTo_FiltersByIssueType()
    {
        var f = new Domain.CustomField("sprint_x", "X", CustomFieldType.Text);
        var projectId = Guid.NewGuid();
        var issueTypeA = Guid.NewGuid();
        var issueTypeB = Guid.NewGuid();

        f.AddContext("Story-only", isGlobal: false, isRequired: false, defaultValueJson: null,
            projectIds: new[] { projectId },
            issueTypeIds: new[] { issueTypeA });

        f.ResolveContext(projectId, issueTypeA).Should().NotBeNull();
        f.ResolveContext(projectId, issueTypeB).Should().BeNull();
    }

    [Fact]
    public void System_CannotBeDeleted()
    {
        var f = new Domain.CustomField("summary", "Summary", CustomFieldType.Text, isSystem: true);
        var act = () => f.EnsureCanDelete();
        act.Should().Throw<DomainException>().Where(ex => ex.Code == CustomFieldErrors.CannotDeleteSystem);
    }
}
