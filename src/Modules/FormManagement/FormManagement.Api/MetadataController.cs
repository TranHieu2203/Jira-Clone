using BB.Web;
using FormManagement.Application;
using FormManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormManagement.Api;

[ApiController]
[Route("api/v1/form-management/metadata")]
[Authorize]
public sealed class MetadataController : BaseController
{
    private readonly IMetadataService _service;
    public MetadataController(IMetadataService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] string? group, CancellationToken ct) =>
        ToResponse(await _service.SearchAsync(keyword, group, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMetadataRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMetadataRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));
}
