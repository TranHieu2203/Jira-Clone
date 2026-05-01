using BB.Common;
using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application;
using Workflow.Application.Engine;

namespace Workflow.Api;

[ApiController]
[Route("api/v1/transitions")]
[Authorize]
public sealed class TransitionsController : BaseController
{
    private readonly IWorkflowEngine _engine;

    public TransitionsController(IWorkflowEngine engine) => _engine = engine;

    /// <summary>List các transition khả thi cho một issue tại trạng thái hiện tại.</summary>
    [HttpGet("available")]
    public async Task<IActionResult> Available(
        [FromQuery] Guid projectId,
        [FromQuery] Guid issueTypeId,
        [FromQuery] Guid currentStatusId,
        [FromQuery] Guid currentUserId,
        CancellationToken ct)
    {
        var result = await _engine.GetAvailableTransitionsAsync(projectId, issueTypeId, currentStatusId, currentUserId, ct);
        if (!result.IsSuccess) return ToResponse(result);

        var dtos = result.Data!
            .Select(a => new AvailableTransitionDto(a.Id, a.Name, a.ToStatusId, a.ToStatusName, a.ScreenId))
            .ToList();
        return ToResponse(Result.Success<IReadOnlyList<AvailableTransitionDto>>(dtos));
    }

    /// <summary>Thực thi 1 transition. Engine sẽ chạy rules → validators → post-functions.</summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] TransitionExecuteRequest request, CancellationToken ct)
    {
        var result = await _engine.TransitionAsync(
            request.IssueId,
            request.ProjectId,
            request.IssueTypeId,
            request.CurrentStatusId,
            request.TransitionId,
            request.Inputs,
            request.Comment,
            ct);
        return ToResponse(result);
    }
}
