using MediatR;
using SharedKernel.Contracts;
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
        // 1. Reserve Stock
        var reservationId = await catalogService.ReserveStockAsync(request.ItemId, request.Quantity, ct);
        
        Order? order = null;
        bool orderSavedToDb = false;

        try
        {
            // 2. Create Order Entity
            order = Order.Create(request.CustomerId, reservationId);
            var mockPrice = new Money(99.99m, "USD"); 
            order.AddItem(request.ItemId, "Mock Product Name", mockPrice, request.Quantity);

            // 3. Save to Database
            context.Orders.Add(order);
            await context.SaveChangesAsync(ct); 
            orderSavedToDb = true; // Mark that EF Core has committed this to the DB

            // 4. Confirm Reservation
            // IF THE TAB CLOSES HERE, WE JUMP TO CATCH
            await catalogService.ConfirmReservationAsync(reservationId, ct);

            // 5. Publish Event
            await publisher.Publish(new OrderCreatedIntegrationEvent(order.Id, request.CustomerId), ct);

            return order.Id;
        }
        catch (Exception)
        {
            // WARNING: The original 'ct' might be canceled! 
            // We MUST use CancellationToken.None to ensure these cleanups actually run.

            // Rollback 1: Release the stock in the Catalog
            await catalogService.ReleaseReservationAsync(reservationId, CancellationToken.None);

            // Rollback 2: Fail the Order if it was already saved to the DB
            if (orderSavedToDb && order != null)
            {
                order.MarkAsFailed("System error or user disconnected before confirmation.");
                
                // We use context.Update or just SaveChanges since the entity is still tracked
                await context.SaveChangesAsync(CancellationToken.None); 
            }

            throw; // Rethrow to let the GlobalExceptionHandler return a 500/400 to the client
        }
    }
}