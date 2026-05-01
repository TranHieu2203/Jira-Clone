using System.Text.Json;
using FluentAssertions;
using Workflow.Application.Engine;
using Workflow.Application.Engine.BuiltIn;

namespace Workflow.UnitTests;

public class BuiltInValidatorsTests
{
    [Fact]
    public async Task FieldRequired_MissingField_ReturnsError()
    {
        var v = new FieldRequiredValidator();
        var ctx = new TransitionContext { Inputs = new Dictionary<string, JsonElement>() };
        var config = JsonDocument.Parse("""{"fields":["assignee"]}""").RootElement;

        var result = await v.ValidateAsync(ctx, config);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Field == "assignee");
    }

    [Fact]
    public async Task FieldRequired_EmptyString_ReturnsError()
    {
        var v = new FieldRequiredValidator();
        var inputs = new Dictionary<string, JsonElement>
        {
            ["assignee"] = JsonDocument.Parse("\"\"").RootElement
        };
        var ctx = new TransitionContext { Inputs = inputs };
        var config = JsonDocument.Parse("""{"fields":["assignee"]}""").RootElement;

        var result = await v.ValidateAsync(ctx, config);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task FieldRequired_PresentField_Passes()
    {
        var v = new FieldRequiredValidator();
        var inputs = new Dictionary<string, JsonElement>
        {
            ["assignee"] = JsonDocument.Parse("\"user-1\"").RootElement
        };
        var ctx = new TransitionContext { Inputs = inputs };
        var config = JsonDocument.Parse("""{"fields":["assignee"]}""").RootElement;

        var result = await v.ValidateAsync(ctx, config);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RegexMatch_FailingPattern_ReturnsError()
    {
        var v = new RegexMatchValidator();
        var inputs = new Dictionary<string, JsonElement>
        {
            ["summary"] = JsonDocument.Parse("\"hi\"").RootElement
        };
        var ctx = new TransitionContext { Inputs = inputs };
        var config = JsonDocument.Parse("""{"field":"summary","pattern":"^.{5,}$"}""").RootElement;

        var result = await v.ValidateAsync(ctx, config);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResolutionRequired_NoResolution_ReturnsError()
    {
        var v = new ResolutionRequiredValidator();
        var ctx = new TransitionContext { Inputs = new Dictionary<string, JsonElement>() };
        var config = JsonDocument.Parse("{}").RootElement;

        var result = await v.ValidateAsync(ctx, config);
        result.IsSuccess.Should().BeFalse();
    }
}
