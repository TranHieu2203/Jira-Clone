using System.Text.Json;
using BB.Security;

namespace Workflow.Application.Engine.BuiltIn;

/// <summary>Pass khi current user có project permission `permission` (config: { "permission": "..." }).</summary>
public sealed class PermissionRule : ITransitionRule
{
    public const string Key = "PERMISSION_RULE";
    public string TypeKey => Key;

    private readonly IPermissionChecker _permissions;
    public PermissionRule(IPermissionChecker permissions) => _permissions = permissions;

    public Task<bool> EvaluateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        if (!config.TryGetProperty("permission", out var p)) return Task.FromResult(false);
        var perm = p.GetString();
        return string.IsNullOrWhiteSpace(perm)
            ? Task.FromResult(false)
            : _permissions.HasProjectPermissionAsync(ctx.CurrentUserId, ctx.ProjectId, perm, ct);
    }
}

/// <summary>Pass khi current user là assignee. Config: { "assigneeFieldKey": "assignee" } — engine đọc trong context inputs nếu có.</summary>
public sealed class UserIsAssigneeRule : ITransitionRule
{
    public const string Key = "USER_IS_ASSIGNEE";
    public string TypeKey => Key;

    public Task<bool> EvaluateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var fieldKey = config.TryGetProperty("assigneeFieldKey", out var f) ? f.GetString() ?? "assignee" : "assignee";
        if (!ctx.Inputs.TryGetValue(fieldKey, out var assigneeRaw)) return Task.FromResult(false);
        if (!Guid.TryParse(assigneeRaw.ValueKind == JsonValueKind.String ? assigneeRaw.GetString() : null, out var assigneeId))
            return Task.FromResult(false);
        return Task.FromResult(assigneeId == ctx.CurrentUserId);
    }
}

/// <summary>Pass khi current user thuộc role config (project-scoped). Config: { "role": "PROJECT_ADMIN" }.</summary>
public sealed class UserInRoleRule : ITransitionRule
{
    public const string Key = "USER_IN_ROLE";
    public string TypeKey => Key;

    private readonly IPermissionChecker _permissions;
    public UserInRoleRule(IPermissionChecker permissions) => _permissions = permissions;

    public Task<bool> EvaluateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        if (!config.TryGetProperty("role", out var r)) return Task.FromResult(false);
        var role = r.GetString();
        return string.IsNullOrWhiteSpace(role)
            ? Task.FromResult(false)
            : _permissions.IsInRoleAsync(ctx.CurrentUserId, ctx.ProjectId, role, ct);
    }
}
