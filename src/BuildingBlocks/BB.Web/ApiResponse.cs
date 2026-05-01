using BB.Common;

namespace BB.Web;

public sealed record ApiError(string Code, string MessageKey, string? Field = null, object? Args = null);

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? MessageKey,
    object? MessageArgs,
    IReadOnlyList<ApiError>? Errors,
    string TraceId,
    DateTimeOffset Timestamp);

public static class ApiResponseFactory
{
    public static ApiResponse<T> From<T>(Result<T> result, string traceId)
    {
        var errors = result.Errors.Count == 0
            ? null
            : result.Errors.Select(e => new ApiError(e.Code, e.MessageKey, e.Field, e.Args)).ToList();
        return new ApiResponse<T>(
            result.IsSuccess,
            result.Data,
            result.MessageKey,
            result.MessageArgs,
            errors,
            traceId,
            DateTimeOffset.UtcNow);
    }

    public static ApiResponse<object?> From(Result result, string traceId)
    {
        var errors = result.Errors.Count == 0
            ? null
            : result.Errors.Select(e => new ApiError(e.Code, e.MessageKey, e.Field, e.Args)).ToList();
        return new ApiResponse<object?>(
            result.IsSuccess,
            null,
            result.MessageKey,
            result.MessageArgs,
            errors,
            traceId,
            DateTimeOffset.UtcNow);
    }

    public static ApiResponse<T> Success<T>(T data, string traceId, string? messageKey = null, object? args = null) =>
        new(true, data, messageKey, args, null, traceId, DateTimeOffset.UtcNow);

    public static ApiResponse<object?> Error(string messageKey, IReadOnlyList<ApiError>? errors, string traceId, object? args = null) =>
        new(false, null, messageKey, args, errors, traceId, DateTimeOffset.UtcNow);
}

public static class HttpStatusMapper
{
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.None => 200,
        ErrorType.Validation => 400,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Unexpected => 500,
        _ => 500
    };
}
