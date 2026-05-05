using BB.Common;
using BB.Web;
using CustomField.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomField.Api;

[ApiController]
[Route("api/v1/custom-fields/demo")]
[Authorize]
public sealed class DemoCustomFieldsAdminController : BaseController
{
    private readonly IDemoCustomFieldProjectBinder _binder;

    public DemoCustomFieldsAdminController(IDemoCustomFieldProjectBinder binder) => _binder = binder;

    /// <summary>
    /// Bind demo custom-field contexts to a project (best-effort, idempotent).
    /// Useful for existing projects created before binder fix/seed.
    /// </summary>
    [HttpPost("bind-project/{projectId:guid}")]
    public async Task<IActionResult> BindProject(Guid projectId, CancellationToken ct = default)
    {
        await _binder.EnsureContextsForProjectAsync(projectId, ct);
        return ToResponse(Result.Success(messageKey: "custom_field.demo.bind.success"));
    }
}

