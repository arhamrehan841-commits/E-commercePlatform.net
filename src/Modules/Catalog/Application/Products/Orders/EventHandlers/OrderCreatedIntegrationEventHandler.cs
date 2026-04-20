using MediatR;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Messaging.Events;

namespace Modules.Catalog.Application.Orders.EventHandlers;

internal sealed class OrderCreatedIntegrationEventHandler(ILogger<OrderCreatedIntegrationEventHandler> logger) 
    : INotificationHandler<OrderCreatedIntegrationEvent>
{
    public Task Handle(OrderCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[CATALOG] Order {OrderId} created for Customer {CustomerId}. Stock reservation is permanently locked.", 
            notification.OrderId, 
            notification.CustomerId);

        return Task.CompletedTask;
    }
}