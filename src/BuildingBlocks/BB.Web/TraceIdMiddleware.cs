using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BB.Web;

public sealed class TraceIdMiddleware
{
    public const string HeaderName = "X-Trace-Id";

    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ICorrelationContext correlation)
    {
        var traceId = ResolveTraceId(ctx);
        Activity.Current?.SetTag("trace_id", traceId);
        ctx.TraceIdentifier = traceId;
        correlation.SetTraceId(traceId);
        ctx.Response.Headers[HeaderName] = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(ctx);
        }
    }

    private static string ResolveTraceId(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue(HeaderName, out var inbound) && !string.IsNullOrWhiteSpace(inbound))
        {
            return inbound.ToString();
        }
        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
