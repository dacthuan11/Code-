using Microsoft.AspNetCore.SignalR;
using Nexus.AuctionService.API.Hubs;
using Nexus.AuctionService.Application.Services;

namespace Nexus.AuctionService.API.Services;

public class AuctionNotifierImpl : IAuctionNotifier
{
    private readonly IHubContext<AuctionHub> _hub;
    public AuctionNotifierImpl(IHubContext<AuctionHub> hub) => _hub = hub;

    public async Task NotifyNewBid(Guid auctionId, Guid bidderId, decimal amount, CancellationToken ct = default)
        => await _hub.Clients.Group($"auction-{auctionId}")
            .SendAsync("NewBid", new { auctionId, bidderId, amount, timestamp = DateTime.UtcNow }, ct);

    public async Task NotifyAuctionEnded(Guid auctionId, Guid? winnerId, decimal finalPrice, CancellationToken ct = default)
        => await _hub.Clients.Group($"auction-{auctionId}")
            .SendAsync("AuctionEnded", new { auctionId, winnerId, finalPrice, timestamp = DateTime.UtcNow }, ct);
}
