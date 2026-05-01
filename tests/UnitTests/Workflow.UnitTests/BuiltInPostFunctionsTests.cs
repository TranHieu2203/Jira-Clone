using System.Text.Json;
using FluentAssertions;
using Workflow.Application.Engine;
using Workflow.Application.Engine.BuiltIn;

namespace Workflow.UnitTests;

public class BuiltInPostFunctionsTests
{
    [Fact]
    public async Task AssignToCurrentUser_WritesAssigneeToFieldChanges()
    {
        var pf = new AssignToCurrentUserPostFunction();
        var userId = Guid.NewGuid();
        var ctx = new TransitionContext { CurrentUserId = userId };
        var config = JsonDocument.Parse("{}").RootElement;

        await pf.ExecuteAsync(ctx, config);

        ctx.FieldChanges.Should().ContainKey("assignee");
        ctx.FieldChanges["assignee"].GetString().Should().Be(userId.ToString());
    }

    [Fact]
    public async Task SetFieldValue_WritesConfiguredValue()
    {
        var pf = new SetFieldValuePostFunction();
        var ctx = new TransitionContext();
        var config = JsonDocument.Parse("""{"field":"priority","value":"high"}""").RootElement;

        await pf.ExecuteAsync(ctx, config);

        ctx.FieldChanges.Should().ContainKey("priority");
        ctx.FieldChanges["priority"].GetString().Should().Be("high");
    }

    [Fact]
    public async Task ClearField_WritesNull()
    {
        var pf = new ClearFieldPostFunction();
        var ctx = new TransitionContext();
        var config = JsonDocument.Parse("""{"field":"resolution"}""").RootElement;

        await pf.ExecuteAsync(ctx, config);

        ctx.FieldChanges["resolution"].ValueKind.Should().Be(JsonValueKind.Null);
    }
}
