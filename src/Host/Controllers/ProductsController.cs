using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Catalog.Application.Products.Create;
using Modules.Catalog.Application.Products.Get.GetById;
using Modules.Catalog.Application.Products.Get.GetProducts;

namespace Host.Controllers;

[ApiController]
[Route("api/catalog/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        // 1. Write Side: Mutate state and get the new ID
        var productId = await _sender.Send(command, ct);

        // 2. Read Side: Re-use the existing Query logic to fetch the full DTO
        // This ensures the response matches our single source of truth for 'ProductResponse'
        var response = await _sender.Send(new GetProductByIdQuery(productId), ct);

        // 3. REST Contract: 201 Created + Location Header + Full Body
        return CreatedAtAction(nameof(GetProduct), new { id = productId }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProductByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet] 
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken ct = default)
    {
        var query = new GetProductsQuery(
            pageNumber, pageSize, searchTerm, category, minPrice, maxPrice, sortBy, sortDescending);
            
        var result = await _sender.Send(query, ct);
        
        return Ok(result);
    }
}