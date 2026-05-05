using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application;

namespace Notification.Api;

/// <summary>
/// R6: per-user email preference (opt-out flags). Mỗi authenticated user
/// quản lý preference của riêng mình; admin không có ghi đè ở MVP.
/// </summary>
[ApiController]
[Route("api/v1/me/email-preferences")]
[Authorize]
public sealed class EmailPreferencesController : BaseController
{
    private readonly IEmailPreferenceService _service;

    public EmailPreferencesController(IEmailPreferenceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct = default) =>
        ToResponse(await _service.GetMineAsync(ct));

    [HttpPut]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateEmailPreferenceRequest request, CancellationToken ct = default) =>
        ToResponse(await _service.UpdateMineAsync(request, ct));
}
