using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Orders.Application.Create;

namespace Modules.Orders.Presentation;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var orderId = await sender.Send(command);
        return Created($"/api/orders/{orderId}", new { Id = orderId });
    }
}