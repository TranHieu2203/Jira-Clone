using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprint.Application;

namespace Sprint.Api;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/sprints")]
[Authorize]
public sealed class SprintsController : BaseController
{
    private readonly ISprintService _service;
    public SprintsController(ISprintService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct) =>
        ToResponse(await _service.ListByProjectAsync(projectId, ct));

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(Guid projectId, CancellationToken ct) =>
        ToResponse(await _service.GetActiveAsync(projectId, ct));

    [HttpGet("{sprintId:guid}")]
    public async Task<IActionResult> GetById(Guid projectId, Guid sprintId, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(projectId, sprintId, ct));

    [HttpGet("{sprintId:guid}/burndown")]
    public async Task<IActionResult> Burndown(Guid projectId, Guid sprintId, CancellationToken ct) =>
        ToResponse(await _service.GetBurndownAsync(projectId, sprintId, ct));

    /// <summary>F7: lịch sử velocity (committed vs completed SP) cho N sprint completed gần nhất.</summary>
    [HttpGet("velocity")]
    public async Task<IActionResult> Velocity(Guid projectId, [FromQuery] int count = 6, CancellationToken ct = default) =>
        ToResponse(await _service.GetVelocityAsync(projectId, count, ct));

    [HttpPost]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateSprintRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(projectId, request, ct));

    [HttpPut("{sprintId:guid}")]
    public async Task<IActionResult> Update(Guid projectId, Guid sprintId, [FromBody] UpdateSprintRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(projectId, sprintId, request, ct));

    [HttpPost("{sprintId:guid}/start")]
    public async Task<IActionResult> Start(Guid projectId, Guid sprintId, CancellationToken ct) =>
        ToResponse(await _service.StartAsync(projectId, sprintId, ct));

    [HttpPost("{sprintId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid projectId, Guid sprintId, CancellationToken ct) =>
        ToResponse(await _service.CompleteAsync(projectId, sprintId, ct));

    [HttpPost("{sprintId:guid}/issues/{issueId:guid}")]
    public async Task<IActionResult> AddIssue(Guid projectId, Guid sprintId, Guid issueId, CancellationToken ct) =>
        ToResponse(await _service.AddIssueAsync(projectId, sprintId, issueId, ct));

    [HttpDelete("{sprintId:guid}/issues/{issueId:guid}")]
    public async Task<IActionResult> RemoveIssue(Guid projectId, Guid sprintId, Guid issueId, CancellationToken ct) =>
        ToResponse(await _service.RemoveIssueAsync(projectId, sprintId, issueId, ct));

    [HttpPut("{sprintId:guid}/issues/order")]
    public async Task<IActionResult> Reorder(Guid projectId, Guid sprintId, [FromBody] ReorderSprintIssuesRequest request, CancellationToken ct) =>
        ToResponse(await _service.ReorderIssuesAsync(projectId, sprintId, request, ct));
}
