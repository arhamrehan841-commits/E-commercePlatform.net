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

        // 2. If ANY item failed, release the successful ones and tell the user
        if (!result.AllReserved)
        {
            if (result.ReservationIds.Any())
            {
                await catalogService.ReleaseReservationsAsync(result.ReservationIds, CancellationToken.None);
            }

            // We throw a custom exception that the API can turn into a 422 Unprocessable Entity
            // This contains the list the frontend needs to show the "Please remove these" prompt
            throw new StockValidationException(result.Rejections);
        }

        Order? order = null;
        bool orderSavedToDb = false;

        try
        {
            // 3. Create and Save (Standard flow since we know all stock is locked)
            order = Order.Create(request.CustomerId, result.ReservationIds);
            
            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, "Fetched Name", new Money(10, "USD"), item.Quantity);
            }

            context.Orders.Add(order);
            await context.SaveChangesAsync(ct);
            orderSavedToDb = true;

            await catalogService.ConfirmReservationsAsync(result.ReservationIds, ct);
            await publisher.Publish(new OrderCreatedIntegrationEvent(order.Id, request.CustomerId), ct);

            return order.Id;
        }
        catch (Exception)
        {
            await catalogService.ReleaseReservationsAsync(result.ReservationIds, CancellationToken.None);
            if (orderSavedToDb && order != null)
            {
                order.MarkAsFailed("Technical failure during strict fulfillment.");
                await context.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
    }
}