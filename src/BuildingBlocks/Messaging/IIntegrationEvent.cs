using MediatR;

namespace BuildingBlocks.Messaging;

// This marker interface ensures all our cross-module events are MediatR notifications
public interface IIntegrationEvent : INotification
{
}