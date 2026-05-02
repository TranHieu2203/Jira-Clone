using ActivityLog.Application;
using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivityLog.Api;

[ApiController]
[Route("api/v1/activity")]
[Authorize]
public sealed class ActivityLogController : BaseController
{
    private readonly IActivityLogService _service;

    public ActivityLogController(IActivityLogService service) => _service = service;

    [HttpGet("by-issue/{issueId:guid}")]
    public async Task<IActionResult> ListByIssue(Guid issueId,
        [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        ToResponse(await _service.ListByIssueAsync(issueId, pageIndex, pageSize, ct));
}
