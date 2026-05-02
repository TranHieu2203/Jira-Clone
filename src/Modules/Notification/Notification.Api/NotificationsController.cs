using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application;

namespace Notification.Api;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : BaseController
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default) =>
        ToResponse(await _service.ListMineAsync(pageIndex, pageSize, unreadOnly, ct));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct = default) =>
        ToResponse(await _service.UnreadCountAsync(ct));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default) =>
        ToResponse(await _service.MarkReadAsync(id, ct));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default) =>
        ToResponse(await _service.MarkAllReadAsync(ct));
}
