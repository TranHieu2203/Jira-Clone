using System.Text.Json;
using System.Text.RegularExpressions;
using BB.Common;

namespace Workflow.Application.Engine.BuiltIn;

/// <summary>Yêu cầu các field được điền trong inputs. Config: { "fields": ["assignee","resolution"] }.</summary>
public sealed class FieldRequiredValidator : ITransitionValidator
{
    public const string Key = "FIELD_REQUIRED";
    public string TypeKey => Key;

    public Task<Result> ValidateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var errors = new List<ResultError>();
        if (config.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in fields.EnumerateArray())
            {
                var name = f.GetString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var hasValue = ctx.Inputs.TryGetValue(name, out var val) && IsNonEmpty(val);
                if (!hasValue)
                    errors.Add(new ResultError(
                        $"FIELD_REQUIRED:{name.ToUpperInvariant()}",
                        "validation.required",
                        Field: name));
            }
        }
        return Task.FromResult(errors.Count == 0
            ? Result.Success()
            : Result.Failure(ErrorType.Validation, "workflow.transition.invalid", errors));
    }

    private static bool IsNonEmpty(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => false,
        JsonValueKind.String => !string.IsNullOrWhiteSpace(v.GetString()),
        JsonValueKind.Array => v.GetArrayLength() > 0,
        _ => true
    };
}

/// <summary>Validate regex trên 1 field. Config: { "field": "summary", "pattern": "^.{5,}$" }.</summary>
public sealed class RegexMatchValidator : ITransitionValidator
{
    public const string Key = "REGEX_MATCH";
    public string TypeKey => Key;

    public Task<Result> ValidateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var fieldName = config.TryGetProperty("field", out var f) ? f.GetString() : null;
        var pattern = config.TryGetProperty("pattern", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(pattern))
            return Task.FromResult(Result.Success());

        if (!ctx.Inputs.TryGetValue(fieldName, out var raw) || raw.ValueKind != JsonValueKind.String)
            return Task.FromResult(Result.Success()); // không có giá trị thì bỏ qua (FIELD_REQUIRED check riêng)

        var value = raw.GetString() ?? string.Empty;
        if (!Regex.IsMatch(value, pattern))
        {
            return Task.FromResult(Result.Failure(
                ErrorType.Validation, "workflow.transition.invalid",
                new[] { new ResultError($"FIELD_REGEX:{fieldName.ToUpperInvariant()}", "validation.regex", Field: fieldName) }));
        }
        return Task.FromResult(Result.Success());
    }
}

/// <summary>Yêu cầu Resolution được set khi vào status Done. Config: { "field": "resolution" }.</summary>
public sealed class ResolutionRequiredValidator : ITransitionValidator
{
    public const string Key = "RESOLUTION_REQUIRED";
    public string TypeKey => Key;

    public Task<Result> ValidateAsync(TransitionContext ctx, JsonElement config, CancellationToken ct = default)
    {
        var field = config.TryGetProperty("field", out var f) ? f.GetString() ?? "resolution" : "resolution";
        var has = ctx.Inputs.TryGetValue(field, out var v)
                  && v.ValueKind == JsonValueKind.String
                  && !string.IsNullOrWhiteSpace(v.GetString());
        return Task.FromResult(has
            ? Result.Success()
            : Result.Failure(ErrorType.Validation, "workflow.transition.invalid",
                new[] { new ResultError("RESOLUTION_REQUIRED", "validation.resolution_required", Field: field) }));
    }
}
