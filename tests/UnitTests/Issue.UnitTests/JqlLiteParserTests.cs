using BB.Common;
using FluentAssertions;
using Issue.Application;

namespace Issue.UnitTests;

public sealed class JqlLiteParserTests
{
    private static readonly Guid User = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void Empty_returns_neutral()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse(null, User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.HasAssigneeClause.Should().BeFalse();
    }

    [Fact]
    public void Assignee_currentUser_maps_user()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("assignee = currentUser()", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.HasAssigneeClause.Should().BeTrue();
        r.Data.AssigneeId.Should().Be(User);
        r.Data.AssigneeUnassignedOnly.Should().BeFalse();
    }

    [Fact]
    public void Assignee_empty_sets_flag()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("assignee = empty", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.AssigneeUnassignedOnly.Should().BeTrue();
        r.Data.AssigneeId.Should().BeNull();
    }

    [Fact]
    public void And_combines_clauses()
    {
        Guid sid = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Result<JqlLiteResult> r = JqlLiteParser.Parse(
            $"assignee = currentUser() AND status = \"{sid}\" AND text ~ \"hello\"",
            User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.AssigneeId.Should().Be(User);
        r.Data.StatusId.Should().Be(sid);
        r.Data.TextContains.Should().Be("hello");
    }
}
