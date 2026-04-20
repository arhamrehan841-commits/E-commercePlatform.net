using MediatR;

namespace Modules.Orders.Application.Create;

public record CreateOrderCommand(Guid CustomerId) : IRequest<Guid>;