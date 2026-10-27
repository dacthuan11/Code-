namespace Nexus.Shared.Messaging;

/// <summary>
/// Interface cho event bus — abstraction layer giữa domain logic và message broker.
/// Dev dùng InMemoryEventBus (chỉ log), prod dùng MassTransit + RabbitMQ.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish một integration event. Constraint: T phải là BaseIntegrationEvent.
    /// </summary>
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : BaseIntegrationEvent;
}
