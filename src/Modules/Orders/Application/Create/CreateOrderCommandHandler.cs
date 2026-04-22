using MediatR;
using SharedKernel.Contracts;
using SharedKernel.Exceptions;
using BuildingBlocks.Messaging.Events;
using Modules.Orders.Domain;
using Modules.Orders.Infrastructure.Data;
using SharedKernel.ValueObjects;

namespace Modules.Orders.Application.Create;

internal sealed class CreateOrderCommandHandler(
    IStockReservationContract catalogService,
    IPublisher publisher,
    OrdersDbContext context) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. Try to reserve everything
        var result = await catalogService.ReserveStockStrictAsync(
            request.Items.Select(i => new BulkReservationRequest(i.ProductId, i.Quantity)), ct);

        // Extract a flat list of IDs for the Confirm/Release methods
        var allReservationIds = result.Reservations.Select(r => r.ReservationId).ToList(); // <-- ADDED

        if (!result.AllReserved)
        {
            if (allReservationIds.Any())
            {
                await catalogService.ReleaseReservationsAsync(allReservationIds, CancellationToken.None);
            }
            throw new StockValidationException(result.Rejections);
        }

        Order? order = null;
        bool orderSavedToDb = false;

        try
        {
            order = Order.Create(request.CustomerId); // <-- UPDATED
            
            foreach (var item in request.Items)
            {
                // Find the reservation that matches this product
                var reservationId = result.Reservations.First(r => r.ItemId == item.ProductId).ReservationId; // <-- ADDED
                
                order.AddItem(item.ProductId, reservationId, "Fetched Name", new Money(10, "USD"), item.Quantity); // <-- UPDATED
            }

            context.Orders.Add(order);
            await context.SaveChangesAsync(ct);
            orderSavedToDb = true;

            await catalogService.ConfirmReservationsAsync(allReservationIds, ct); // <-- UPDATED
            await publisher.Publish(new OrderCreatedIntegrationEvent(order.Id, request.CustomerId), ct);

            return order.Id;
        }
        catch (Exception)
        {
            await catalogService.ReleaseReservationsAsync(allReservationIds, CancellationToken.None); // <-- UPDATED
            if (orderSavedToDb && order != null)
            {
                order.MarkAsFailed("Technical failure during strict fulfillment.");
                await context.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
    }
}