using MassTransit;
using Microsoft.Extensions.Logging;

namespace Nexus.Shared.Messaging;

/// <summary>
/// IEventBus implementation that uses MassTransit IPublishEndpoint.
/// Works with both InMemory (dev) and RabbitMQ (prod) transports.
/// In dev mode with InMemory transport, events are dispatched within the same process.
/// In prod mode with RabbitMQ, events cross service boundaries.
/// </summary>
public class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventBus> _logger;

    public MassTransitEventBus(IPublishEndpoint publishEndpoint, ILogger<MassTransitEventBus> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : BaseIntegrationEvent
    {
        _logger.LogInformation("[EventBus] Publishing {EventType}: {EventId}",
            typeof(T).Name, integrationEvent.EventId);
        await _publishEndpoint.Publish(integrationEvent, ct);
    }
}
