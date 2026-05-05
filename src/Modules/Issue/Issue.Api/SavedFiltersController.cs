using BB.Web;
using Issue.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issue.Api;

[ApiController]
[Route("api/v1/saved-filters")]
[Authorize]
public sealed class SavedFiltersController : BaseController
{
    private readonly ISavedFilterService _service;
    public SavedFiltersController(ISavedFilterService service) => _service = service;

    /// <summary>List filter của current user + filter shared.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(CancellationToken ct = default) =>
        ToResponse(await _service.ListMineAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavedFilterRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSavedFilterRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));
}
