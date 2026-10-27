using MassTransit;
using Microsoft.Extensions.Logging;
using Nexus.Shared.Events;

namespace Nexus.UserService.Application.Consumers;

/// <summary>
/// Dev-mode consumer: handles UserRegisteredEvent within UserService process.
/// Simulates the welcome notification that NotificationService would send in prod (RabbitMQ).
/// In prod, this event is handled by NotificationService.UserRegisteredConsumer instead.
/// </summary>
public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "[Notification] Welcome email sent to {Email} (UserId: {UserId}, Name: {Name})",
            evt.Email, evt.UserId, evt.FullName);
        return Task.CompletedTask;
    }
}
