using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sample.Application;

namespace Sample.Api;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public sealed class ProductsController : BaseController
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ProductFilter filter, CancellationToken ct)
    {
        var result = await _service.SearchAsync(filter, ct);
        return ToResponse(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));
}
