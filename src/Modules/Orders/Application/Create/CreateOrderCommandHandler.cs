using MediatR;
using SharedKernel.Contracts;
using BuildingBlocks.Messaging.Events;
// using Modules.Orders.Domain; (Assuming you have an Order entity)
// using Modules.Orders.Infrastructure.Data;

namespace Modules.Orders.Application.Create;

internal sealed class CreateOrderCommandHandler(
    IStockReservationContract catalogService,
    IPublisher publisher
    /* OrdersDbContext context */) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. Reserve Stock (Cross-Module Sync Call)
        var reservationId = await catalogService.ReserveStockAsync(request.ItemId, request.Quantity, ct);

        try
        {
            // 2. Save Order with ReservationId
            var orderId = Guid.NewGuid();
            /* var order = new Order { Id = orderId, CustomerId = request.CustomerId, ReservationId = reservationId };
            context.Orders.Add(order);
            await context.SaveChangesAsync(ct);
            */

            // 3. Confirm Reservation
            await catalogService.ConfirmReservationAsync(reservationId, ct);

            // 4. Publish Integration Event
            await publisher.Publish(new OrderCreatedIntegrationEvent(orderId, request.CustomerId), ct);

            return orderId;
        }
        catch (Exception)
        {
            // Compensating Transaction: Rollback the reservation if anything fails
            await catalogService.ReleaseReservationAsync(reservationId, ct);
            throw;
        }
    }
}