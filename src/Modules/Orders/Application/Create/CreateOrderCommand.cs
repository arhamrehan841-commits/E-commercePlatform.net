using MediatR;

namespace Modules.Orders.Application.Create;

public record CreateOrderCommand(Guid CustomerId, Guid ItemId, int Quantity) : IRequest<Guid>;