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
        r.Data.StatusName.Should().BeNull();
        r.Data.TextContains.Should().Be("hello");
    }

    [Fact]
    public void Status_quoted_non_guid_is_name()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("status = \"In Progress\"", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.HasStatusClause.Should().BeTrue();
        r.Data.StatusId.Should().BeNull();
        r.Data.StatusName.Should().Be("In Progress");
    }

    [Fact]
    public void Cf_string_and_number_clauses()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse(
            "cf[env] = \"prod\" AND cf[points] = 5",
            User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.CustomFieldClauses.Should().HaveCount(2);
        r.Data.CustomFieldClauses[0].FieldKey.Should().Be("env");
        r.Data.CustomFieldClauses[0].StringEquals.Should().Be("prod");
        r.Data.CustomFieldClauses[1].FieldKey.Should().Be("points");
        r.Data.CustomFieldClauses[1].NumberEquals.Should().Be(5);
    }

    [Fact]
    public void Duplicate_cf_key_fails()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("cf[x] = \"a\" AND cf[X] = \"b\"", User);
        r.IsSuccess.Should().BeFalse();
        r.MessageKey.Should().Be("issue.search.jql.duplicate_cf");
    }

    // ─── F1: priority / type / label ─────────────────────────────────

    [Theory]
    [InlineData("priority = High", 4)]
    [InlineData("priority = high", 4)]
    [InlineData("priority = \"Highest\"", 5)]
    [InlineData("priority = Lowest", 1)]
    [InlineData("priority = 3", 3)]
    [InlineData("priority = 5", 5)]
    public void Priority_parses_name_or_number(string jql, int expected)
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse(jql, User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.Priority.Should().Be(expected);
    }

    [Fact]
    public void Priority_invalid_number_fails()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("priority = 9", User);
        r.IsSuccess.Should().BeFalse();
        r.MessageKey.Should().Be("issue.search.jql.unrecognized_clause");
    }

    [Fact]
    public void Priority_duplicate_fails()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("priority = High AND priority = Low", User);
        r.IsSuccess.Should().BeFalse();
        r.MessageKey.Should().Be("issue.search.jql.duplicate_priority");
    }

    [Fact]
    public void Type_captures_key()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("type = \"BUG\"", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.IssueTypeKey.Should().Be("BUG");
    }

    [Fact]
    public void Type_duplicate_fails()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("type = \"BUG\" AND type = \"STORY\"", User);
        r.IsSuccess.Should().BeFalse();
        r.MessageKey.Should().Be("issue.search.jql.duplicate_type");
    }

    [Fact]
    public void Label_equals_single()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("label = \"frontend\"", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.Labels.Should().ContainSingle().Which.Should().Be("frontend");
    }

    [Fact]
    public void Label_in_multi()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("label in (\"frontend\", \"urgent\", \"v2\")", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.Labels.Should().HaveCount(3).And.Contain(new[] { "frontend", "urgent", "v2" });
    }

    [Fact]
    public void Label_in_dedup_case_insensitive()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("label in (\"frontend\", \"FRONTEND\")", User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.Labels.Should().ContainSingle();
    }

    [Fact]
    public void Label_in_empty_fails()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse("label in ()", User);
        r.IsSuccess.Should().BeFalse();
        r.MessageKey.Should().Be("issue.search.jql.label_in_empty");
    }

    [Fact]
    public void All_clauses_combined()
    {
        Result<JqlLiteResult> r = JqlLiteParser.Parse(
            "assignee = currentUser() AND priority = High AND type = \"BUG\" AND label = \"urgent\" AND text ~ \"crash\"",
            User);
        r.IsSuccess.Should().BeTrue();
        r.Data!.AssigneeId.Should().Be(User);
        r.Data.Priority.Should().Be(4);
        r.Data.IssueTypeKey.Should().Be("BUG");
        r.Data.Labels.Should().ContainSingle().Which.Should().Be("urgent");
        r.Data.TextContains.Should().Be("crash");
    }
}
