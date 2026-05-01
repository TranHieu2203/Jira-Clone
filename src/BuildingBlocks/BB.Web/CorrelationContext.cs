namespace BB.Web;

public interface ICorrelationContext
{
    string TraceId { get; }
    void SetTraceId(string traceId);
}

public sealed class CorrelationContext : ICorrelationContext
{
    private string _traceId = string.Empty;
    public string TraceId => _traceId;
    public void SetTraceId(string traceId) => _traceId = traceId;
}
