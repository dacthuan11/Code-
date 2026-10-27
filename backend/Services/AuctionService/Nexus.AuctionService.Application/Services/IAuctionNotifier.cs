namespace Nexus.AuctionService.Application.Services;

public interface IAuctionNotifier
{
    Task NotifyNewBid(Guid auctionId, Guid bidderId, decimal amount, CancellationToken ct = default);
    Task NotifyAuctionEnded(Guid auctionId, Guid? winnerId, decimal finalPrice, CancellationToken ct = default);
}
