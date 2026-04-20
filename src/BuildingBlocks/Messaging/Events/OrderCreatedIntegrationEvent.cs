namespace BuildingBlocks.Messaging.Events;

// This is the payload that will be broadcasted across the system
public record OrderCreatedIntegrationEvent(Guid OrderId, Guid CustomerId) : IIntegrationEvent;