using MassTransit;
using Microsoft.Extensions.Logging;
using Nexus.Shared.Events;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.Consumers;

public class AuctionPaymentTimeoutConsumer : IConsumer<AuctionPaymentTimeoutEvent>
{
    private readonly IUserRepository _users;
    private readonly ILogger<AuctionPaymentTimeoutConsumer> _logger;

    public AuctionPaymentTimeoutConsumer(IUserRepository users, ILogger<AuctionPaymentTimeoutConsumer> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuctionPaymentTimeoutEvent> context)
    {
        var evt = context.Message;
        var user = await _users.GetByIdAsync(evt.WinnerId, context.CancellationToken);
        if (user is null)
        {
            _logger.LogWarning("[PaymentTimeout] User {UserId} not found, skipping penalty", evt.WinnerId);
            return;
        }

        user.ApplyPenalty(evt.PenaltyScore);
        await _users.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("[PaymentTimeout] Applied penalty {Score} to user {UserId}", evt.PenaltyScore, evt.WinnerId);
    }
}
