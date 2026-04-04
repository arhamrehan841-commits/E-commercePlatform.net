using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Catalog.Application.Products.Create;
using Modules.Catalog.Application.Products.GetById;

namespace Host.Controllers;

[ApiController]
[Route("api/catalog/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command, 
        CancellationToken cancellationToken)
    {
        // 1. Send the command to MediatR
        var productId = await _sender.Send(command, cancellationToken);

        // 2. Return a 201 Created with the location of the new resource
        return CreatedAtAction(nameof(GetProduct), new { id = productId }, new { id = productId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(
        [FromRoute] Guid id, 
        CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        // Handle the nullable response contract we explicitly defined on Day 8
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}