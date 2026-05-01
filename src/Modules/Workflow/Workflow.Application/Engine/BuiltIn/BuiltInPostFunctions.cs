using System.Text.Json;

namespace Workflow.Application.Engine.BuiltIn;

/// <summary>Auto-assign issue cho current user. Config: { "field": "assignee" }.</summary>
public sealed class AssignToCurrentUserPostFunction : ITransitionPostFunction
{
    public const string Key = "ASSIGN_TO_CURRENT_USER";
    public string TypeKey => Key;

    public Task ExecuteAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var field = config.TryGetProperty("field", out var f) ? f.GetString() ?? "assignee" : "assignee";
        ctx.FieldChanges[field] = JsonDocument.Parse($"\"{ctx.CurrentUserId}\"").RootElement.Clone();
        return Task.CompletedTask;
    }
}

/// <summary>Set giá trị cố định cho field. Config: { "field": "x", "value": <any> }.</summary>
public sealed class SetFieldValuePostFunction : ITransitionPostFunction
{
    public const string Key = "SET_FIELD_VALUE";
    public string TypeKey => Key;

    public Task ExecuteAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var field = config.TryGetProperty("field", out var f) ? f.GetString() : null;
        if (string.IsNullOrWhiteSpace(field)) return Task.CompletedTask;

        if (config.TryGetProperty("value", out var v))
            ctx.FieldChanges[field] = v.Clone();
        return Task.CompletedTask;
    }
}

/// <summary>Clear giá trị field. Config: { "field": "resolution" }.</summary>
public sealed class ClearFieldPostFunction : ITransitionPostFunction
{
    public const string Key = "CLEAR_FIELD";
    public string TypeKey => Key;

    public Task ExecuteAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var field = config.TryGetProperty("field", out var f) ? f.GetString() : null;
        if (string.IsNullOrWhiteSpace(field)) return Task.CompletedTask;

        ctx.FieldChanges[field] = JsonDocument.Parse("null").RootElement.Clone();
        return Task.CompletedTask;
    }
}
