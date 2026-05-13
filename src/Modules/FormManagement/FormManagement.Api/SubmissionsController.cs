using BB.Web;
using FormManagement.Application;
using FormManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormManagement.Api;

[ApiController]
[Route("api/v1/form-management/submissions")]
[Authorize]
public sealed class SubmissionsController : BaseController
{
    private readonly ISubmissionService _service;
    public SubmissionsController(ISubmissionService service) => _service = service;

    [HttpGet("by-template/{templateId:guid}")]
    public async Task<IActionResult> ListByTemplate(Guid templateId, CancellationToken ct) =>
        ToResponse(await _service.ListByTemplateAsync(templateId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    /// <summary>
    /// Submit data + mail-merge → file output. Trả thẳng FileContentResult cho client download.
    /// Khi merge fail (chưa hỗ trợ ở Phase 2) → trả ApiResponse fail standard.
    /// </summary>
    [HttpPost("submit-and-export")]
    public async Task<IActionResult> SubmitAndExport([FromBody] CreateSubmissionRequest request, CancellationToken ct)
    {
        var result = await _service.SubmitAndExportAsync(request, ct);
        if (result.IsFailure) return ToResponse(result);

        var dto = result.Data!;
        return File(dto.FileBytes, dto.ContentType, dto.FileName);
    }
}
