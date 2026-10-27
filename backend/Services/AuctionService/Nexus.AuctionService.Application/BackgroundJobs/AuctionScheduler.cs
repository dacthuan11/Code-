using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.AuctionService.Domain.Entities;
using Nexus.AuctionService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.AuctionService.Application.BackgroundJobs;

public class AuctionScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AuctionScheduler> _logger;

    public AuctionScheduler(IServiceScopeFactory scopes, ILogger<AuctionScheduler> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuctionScheduler] Error during tick");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var now = DateTime.UtcNow;

        // Auto-activate Scheduled auctions whose StartTime has passed
        var scheduled = await repo.GetScheduledAuctionsAsync(now, ct);
        foreach (var a in scheduled)
        {
            a.Activate();
            _logger.LogInformation("[AuctionScheduler] Activated auction {Id}", a.Id);
            await eventBus.PublishAsync(new AuctionStartedEvent(a.Id, a.StartTime, a.EndTime), ct);
        }

        // Auto-end Active auctions whose EndTime has passed
        var active = await repo.GetActiveAuctionsAsync(ct);
        foreach (var a in active.Where(x => x.EndTime <= now))
        {
            a.End();
            _logger.LogInformation("[AuctionScheduler] Ended auction {Id} winner={Winner}", a.Id, a.CurrentWinnerId);

            if (a.CurrentWinnerId.HasValue)
                await eventBus.PublishAsync(
                    new AuctionWonEvent(a.Id, a.CurrentWinnerId.Value, a.SellerId, a.ProductId, a.CurrentPrice), ct);
            else
                await eventBus.PublishAsync(
                    new AuctionFailedEvent(a.Id, "No bids"), ct);
        }

        // Check for auctions where winner missed payment deadline (24h window)
        var expired = await repo.GetExpiredPaymentDeadlineAuctionsAsync(now, ct);
        foreach (var a in expired)
        {
            if (!a.CurrentWinnerId.HasValue) continue;
            a.ClearPaymentDeadline(); // prevents re-firing
            a.Fail();
            _logger.LogInformation("[AuctionScheduler] Payment timeout for auction {Id} winner={Winner}", a.Id, a.CurrentWinnerId);
            await eventBus.PublishAsync(
                new AuctionPaymentTimeoutEvent(a.Id, a.CurrentWinnerId.Value, 10m), ct);
        }

        if (scheduled.Count + active.Count(x => x.EndTime <= now) + expired.Count > 0)
            await repo.SaveChangesAsync(ct);
    }
}
