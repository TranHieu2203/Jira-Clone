using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application;

namespace Workflow.Api;

[ApiController]
[Route("api/v1/workflows")]
[Authorize]
public sealed class WorkflowsController : BaseController
{
    private readonly IWorkflowService _service;

    public WorkflowsController(IWorkflowService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpGet("by-project/{projectId:guid}")]
    public async Task<IActionResult> ListByProject(Guid projectId, CancellationToken ct) =>
        ToResponse(await _service.ListByProjectAsync(projectId, ct));

    [HttpGet("templates")]
    public async Task<IActionResult> ListTemplates(CancellationToken ct) =>
        ToResponse(await _service.ListTemplatesAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkflowRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));

    // ========== Status ==========
    [HttpPost("{id:guid}/statuses")]
    public async Task<IActionResult> AddStatus(Guid id, [FromBody] AddStatusRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddStatusAsync(id, request, ct));

    [HttpDelete("{id:guid}/statuses/{statusId:guid}")]
    public async Task<IActionResult> RemoveStatus(Guid id, Guid statusId, CancellationToken ct) =>
        ToResponse(await _service.RemoveStatusAsync(id, statusId, ct));

    [HttpPut("{id:guid}/initial-status/{statusId:guid}")]
    public async Task<IActionResult> SetInitialStatus(Guid id, Guid statusId, CancellationToken ct) =>
        ToResponse(await _service.SetInitialStatusAsync(id, statusId, ct));

    // ========== Transition ==========
    [HttpPost("{id:guid}/transitions")]
    public async Task<IActionResult> AddTransition(Guid id, [FromBody] AddTransitionRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddTransitionAsync(id, request, ct));

    [HttpDelete("{id:guid}/transitions/{transitionId:guid}")]
    public async Task<IActionResult> RemoveTransition(Guid id, Guid transitionId, CancellationToken ct) =>
        ToResponse(await _service.RemoveTransitionAsync(id, transitionId, ct));

    // ========== Transition steps ==========
    [HttpPost("{id:guid}/transitions/{transitionId:guid}/rules")]
    public async Task<IActionResult> AddRule(Guid id, Guid transitionId, [FromBody] AddTransitionStepRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddRuleAsync(id, transitionId, request, ct));

    [HttpPost("{id:guid}/transitions/{transitionId:guid}/validators")]
    public async Task<IActionResult> AddValidator(Guid id, Guid transitionId, [FromBody] AddTransitionStepRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddValidatorAsync(id, transitionId, request, ct));

    [HttpPost("{id:guid}/transitions/{transitionId:guid}/post-functions")]
    public async Task<IActionResult> AddPostFunction(Guid id, Guid transitionId, [FromBody] AddTransitionStepRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddPostFunctionAsync(id, transitionId, request, ct));

    [HttpDelete("{id:guid}/transitions/{transitionId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> RemoveStep(Guid id, Guid transitionId, Guid stepId, CancellationToken ct) =>
        ToResponse(await _service.RemoveTransitionStepAsync(id, transitionId, stepId, ct));
}
