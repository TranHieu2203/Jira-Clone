using System.Text.Json;
using BB.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BB.Web;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var traceId = ctx.Response.Headers.TryGetValue(TraceIdMiddleware.HeaderName, out var t)
            ? t.ToString()
            : ctx.TraceIdentifier;

        ApiResponse<object?> body;
        int status;

        switch (ex)
        {
            case ValidationException ve:
                status = 400;
                var errors = ve.Errors
                    .Select(e => new ApiError(
                        Code: e.ErrorCode ?? "VALIDATION_FAILED",
                        MessageKey: e.ErrorMessage,
                        Field: e.PropertyName,
                        Args: e.FormattedMessagePlaceholderValues))
                    .ToList();
                body = ApiResponseFactory.Error("validation.failed", errors, traceId);
                _logger.LogWarning("Validation failed. TraceId={TraceId} Errors={Errors}", traceId, ve.Errors);
                break;

            case DomainException de:
                status = HttpStatusMapper.ToStatusCode(de.ErrorType);
                body = ApiResponseFactory.Error(
                    de.MessageKey,
                    new[] { new ApiError(de.Code, de.MessageKey, null, de.Args) },
                    traceId,
                    de.Args);
                _logger.LogWarning(de, "Domain exception. TraceId={TraceId} Code={Code}", traceId, de.Code);
                break;

            case UnauthorizedAccessException:
                status = 401;
                body = ApiResponseFactory.Error("auth.unauthorized", null, traceId);
                break;

            default:
                status = 500;
                body = ApiResponseFactory.Error("system.unexpected", null, traceId);
                _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);
                break;
        }

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, body, JsonOpts, ct);
        return true;
    }
}
