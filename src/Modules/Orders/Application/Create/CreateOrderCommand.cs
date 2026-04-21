using MediatR;

namespace Modules.Orders.Application.Create;

// Defines the data needed for a single line item
public record OrderItemRequest(Guid ProductId, int Quantity);

// The command now accepts a collection of items
public record CreateOrderCommand(Guid CustomerId, List<OrderItemRequest> Items) : IRequest<Guid>;