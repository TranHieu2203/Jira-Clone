using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application;

namespace Notification.Api;

[ApiController]
[Route("api/v1/admin/email-logs")]
[Authorize(Roles = "Admin")]
public sealed class EmailLogsAdminController : BaseController
{
    private readonly IEmailService _service;

    public EmailLogsAdminController(IEmailService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? templateKey = null,
        [FromQuery] string? toEmail = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default) =>
        ToResponse(await _service.ListLogsAsync(pageIndex, pageSize, templateKey, toEmail, status, ct));

    /// <summary>Admin test send (MVP) — dùng template + args để gửi email và tạo log.</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest req, CancellationToken ct = default) =>
        ToResponse(await _service.SendAsync(req, ct));

    /// <summary>R6 DLQ retry — chỉ cho phép log đang Failed.</summary>
    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct = default) =>
        ToResponse(await _service.RetryAsync(id, ct));
}

