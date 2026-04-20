using MediatR;
namespace Modules.Orders.Application.Create;

internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Mocking a successful order creation for now
        return Task.FromResult(Guid.NewGuid());
    }
}