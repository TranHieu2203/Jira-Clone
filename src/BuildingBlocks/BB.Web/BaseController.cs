using BB.Common;
using Microsoft.AspNetCore.Mvc;

namespace BB.Web;

[ApiController]
public abstract class BaseController : ControllerBase
{
    private string TraceId => HttpContext.Response.Headers.TryGetValue(TraceIdMiddleware.HeaderName, out var t)
        ? t.ToString()
        : HttpContext.TraceIdentifier;

    protected IActionResult ToResponse<T>(Result<T> result)
    {
        var status = result.IsSuccess ? 200 : HttpStatusMapper.ToStatusCode(result.ErrorType);
        return StatusCode(status, ApiResponseFactory.From(result, TraceId));
    }

    protected IActionResult ToResponse(Result result)
    {
        var status = result.IsSuccess ? 200 : HttpStatusMapper.ToStatusCode(result.ErrorType);
        return StatusCode(status, ApiResponseFactory.From(result, TraceId));
    }

    protected IActionResult Created<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return ToResponse(result);
        }
        return StatusCode(201, ApiResponseFactory.From(result, TraceId));
    }
}
