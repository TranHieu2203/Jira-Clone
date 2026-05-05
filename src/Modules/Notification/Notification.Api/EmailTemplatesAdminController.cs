using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application;

namespace Notification.Api;

[ApiController]
[Route("api/v1/admin/email-templates")]
[Authorize(Roles = "Admin")]
public sealed class EmailTemplatesAdminController : BaseController
{
    private readonly IEmailService _service;

    public EmailTemplatesAdminController(IEmailService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? q = null,
        CancellationToken ct = default) =>
        ToResponse(await _service.ListTemplatesAsync(pageIndex, pageSize, q, ct));

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct = default) =>
        ToResponse(await _service.GetTemplateAsync(key, ct));

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertEmailTemplateRequest req, CancellationToken ct = default) =>
        ToResponse(await _service.UpsertTemplateAsync(req, ct));
}

