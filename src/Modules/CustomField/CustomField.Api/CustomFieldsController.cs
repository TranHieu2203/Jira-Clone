using BB.Web;
using CustomField.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomField.Api;

[ApiController]
[Route("api/v1/custom-fields")]
[Authorize]
public sealed class CustomFieldsController : BaseController
{
    private readonly ICustomFieldService _service;
    public CustomFieldsController(ICustomFieldService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        ToResponse(await _service.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpGet("by-key/{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct) =>
        ToResponse(await _service.GetByKeyAsync(key, ct));

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] Guid projectId, [FromQuery] Guid issueTypeId, CancellationToken ct) =>
        ToResponse(await _service.ResolveForAsync(projectId, issueTypeId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomFieldRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomFieldRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));

    // Options
    [HttpPost("{id:guid}/options")]
    public async Task<IActionResult> AddOption(Guid id, [FromBody] AddOptionRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddOptionAsync(id, request, ct));

    [HttpPut("{id:guid}/options/{optionId:guid}")]
    public async Task<IActionResult> UpdateOption(Guid id, Guid optionId, [FromBody] UpdateOptionRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateOptionAsync(id, optionId, request, ct));

    [HttpDelete("{id:guid}/options/{optionId:guid}")]
    public async Task<IActionResult> RemoveOption(Guid id, Guid optionId, CancellationToken ct) =>
        ToResponse(await _service.RemoveOptionAsync(id, optionId, ct));

    // Contexts
    [HttpPost("{id:guid}/contexts")]
    public async Task<IActionResult> AddContext(Guid id, [FromBody] AddContextRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddContextAsync(id, request, ct));

    [HttpDelete("{id:guid}/contexts/{contextId:guid}")]
    public async Task<IActionResult> RemoveContext(Guid id, Guid contextId, CancellationToken ct) =>
        ToResponse(await _service.RemoveContextAsync(id, contextId, ct));
}
