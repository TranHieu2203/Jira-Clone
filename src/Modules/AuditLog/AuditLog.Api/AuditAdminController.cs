using AuditLog.Application;
using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditLog.Api;

/// <summary>
/// Admin-only audit log query endpoint. Dùng [Authorize(Roles="Admin")]
/// nhất quán với EmailTemplatesAdminController + EmailLogsAdminController.
/// </summary>
[ApiController]
[Route("api/v1/admin/audit")]
[Authorize(Roles = "Admin")]
public sealed class AuditAdminController : BaseController
{
    private readonly IAuditQueryService _service;

    public AuditAdminController(IAuditQueryService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? scope = null,
        [FromQuery] Guid? scopeId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var req = new SearchAuditRequest(actorUserId, action, scope, scopeId, from, to, pageIndex, pageSize);
        return ToResponse(await _service.SearchAsync(req, ct));
    }
}
