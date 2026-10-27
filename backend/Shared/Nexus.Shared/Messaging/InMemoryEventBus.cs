using Microsoft.Extensions.Logging;

namespace Nexus.Shared.Messaging;

/// <summary>
/// Implementation của IEventBus dùng cho môi trường dev.
/// Không gửi message thật — chỉ log để developer biết event được publish.
/// Prod sẽ dùng MassTransit với RabbitMQ transport thay thế cái này.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger) => _logger = logger;

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : BaseIntegrationEvent
    {
        // Log thay vì gửi thật — trong dev, ta chỉ cần biết event được fire
        _logger.LogInformation("[EventBus] Published {EventType}: {EventId}",
            typeof(T).Name, integrationEvent.EventId);

        return Task.CompletedTask;
    }
}
