namespace BB.Common;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    Unauthorized = 2,
    Forbidden = 3,
    NotFound = 4,
    Conflict = 5,
    Unexpected = 6
}

public sealed record ResultError(string Code, string MessageKey, string? Field = null, object? Args = null);

public class Result
{
    public bool IsSuccess { get; }
    public ErrorType ErrorType { get; }
    public string? MessageKey { get; }
    public object? MessageArgs { get; }
    public IReadOnlyList<ResultError> Errors { get; }

    protected Result(bool isSuccess, ErrorType errorType, string? messageKey, object? messageArgs, IReadOnlyList<ResultError>? errors)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        MessageKey = messageKey;
        MessageArgs = messageArgs;
        Errors = errors ?? Array.Empty<ResultError>();
    }

    public static Result Success(string? messageKey = null, object? messageArgs = null) =>
        new(true, ErrorType.None, messageKey, messageArgs, null);

    public static Result Failure(ErrorType type, string messageKey, IReadOnlyList<ResultError>? errors = null, object? messageArgs = null) =>
        new(false, type, messageKey, messageArgs, errors);

    public static Result<T> Success<T>(T data, string? messageKey = null, object? messageArgs = null) =>
        Result<T>.Ok(data, messageKey, messageArgs);

    public static Result<T> Failure<T>(ErrorType type, string messageKey, IReadOnlyList<ResultError>? errors = null, object? messageArgs = null) =>
        Result<T>.Fail(type, messageKey, errors, messageArgs);
}

public sealed class Result<T> : Result
{
    public T? Data { get; }

    private Result(T? data, bool isSuccess, ErrorType errorType, string? messageKey, object? messageArgs, IReadOnlyList<ResultError>? errors)
        : base(isSuccess, errorType, messageKey, messageArgs, errors)
    {
        Data = data;
    }

    public static Result<T> Ok(T data, string? messageKey = null, object? messageArgs = null) =>
        new(data, true, ErrorType.None, messageKey, messageArgs, null);

    public static Result<T> Fail(ErrorType type, string messageKey, IReadOnlyList<ResultError>? errors = null, object? messageArgs = null) =>
        new(default, false, type, messageKey, messageArgs, errors);
}
