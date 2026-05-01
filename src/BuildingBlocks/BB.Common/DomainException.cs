namespace BB.Common;

public class DomainException : Exception
{
    public string Code { get; }
    public string MessageKey { get; }
    public ErrorType ErrorType { get; }
    public object? Args { get; }

    public DomainException(string code, string messageKey, ErrorType errorType = ErrorType.Validation, object? args = null)
        : base(messageKey)
    {
        Code = code;
        MessageKey = messageKey;
        ErrorType = errorType;
        Args = args;
    }
}
