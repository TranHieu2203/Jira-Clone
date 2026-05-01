using System.Text.Json;
using BB.Common;

namespace Workflow.Application.Engine;

/// <summary>
/// Context truyền vào mọi rule/validator/post-function khi engine chạy.
/// </summary>
public sealed class TransitionContext
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid IssueTypeId { get; init; }
    public Guid CurrentUserId { get; init; }
    public string CurrentUserName { get; init; } = string.Empty;
    public Guid WorkflowId { get; init; }
    public Guid TransitionId { get; init; }
    public Guid? FromStatusId { get; init; }
    public Guid ToStatusId { get; init; }
    public IReadOnlyDictionary<string, JsonElement> Inputs { get; init; } = new Dictionary<string, JsonElement>();
    public string? Comment { get; init; }

    /// <summary>
    /// Field changes mà engine tích luỹ qua post-functions để sau đó áp dụng vào Issue.
    /// Caller (Issue module) đọc property này sau khi engine return.
    /// </summary>
    public Dictionary<string, JsonElement> FieldChanges { get; } = new();
}

public interface ITransitionRule
{
    string TypeKey { get; }
    Task<bool> EvaluateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default);
}

public interface ITransitionValidator
{
    string TypeKey { get; }
    Task<Result> ValidateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default);
}

public interface ITransitionPostFunction
{
    string TypeKey { get; }
    Task ExecuteAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default);
}
