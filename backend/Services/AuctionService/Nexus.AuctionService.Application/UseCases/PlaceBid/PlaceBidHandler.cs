using MediatR;
using Nexus.AuctionService.Application.Services;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.AuctionService.Domain.Repositories;

namespace Nexus.AuctionService.Application.UseCases.PlaceBid;

// Handler xử lý đặt bid - phức tạp nhất trong service vì phải handle concurrent bids
public class PlaceBidHandler : IRequestHandler<PlaceBidCommand, PlaceBidResult>
{
    private readonly IAuctionRepository _auctionRepo;
    private readonly IEventBus _eventBus;
    private readonly IDistributedLock _distributedLock;
    private readonly IAuctionNotifier _notifier;

    public PlaceBidHandler(IAuctionRepository auctionRepo, IEventBus eventBus,
        IDistributedLock distributedLock, IAuctionNotifier notifier)
    {
        _auctionRepo = auctionRepo;
        _eventBus = eventBus;
        _distributedLock = distributedLock;
        _notifier = notifier;
    }

    public async Task<PlaceBidResult> Handle(PlaceBidCommand cmd, CancellationToken ct)
    {
        // Acquire distributed lock per auction ID - tránh hai bid cùng auction thắng cùng lúc
        // Timeout 5s: đủ để xử lý xong một bid, không block quá lâu nếu lock bị hold
        // `await using` đảm bảo lock được release dù có exception hay không (RAII pattern)
        await using var @lock = await _distributedLock.AcquireAsync($"auction:{cmd.AuctionId}", TimeSpan.FromSeconds(5));

        var auction = await _auctionRepo.GetByIdAsync(cmd.AuctionId, ct)
            ?? throw new KeyNotFoundException($"Auction {cmd.AuctionId} not found.");

        // Lưu lại winner cũ TRƯỚC khi PlaceBid update - cần để notify OutbidEvent cho đúng người
        var previousWinnerId = auction.CurrentWinnerId;
        var bid = auction.PlaceBid(cmd.BidderId, cmd.BidderEmail, cmd.Amount); // business logic nằm trong domain

        // EF Core detects Auction property changes (CurrentPrice, CurrentWinnerId, UpdatedAt, EndTime)
        // via snapshot comparison during SaveChanges, so no explicit UpdateAsync is needed there.
        // However, the new Bid added to auction.Bids is tracked as Modified by EF Core (because it
        // has a non-null Guid key with no configured value generator). Calling AddBidAsync explicitly
        // overrides the Bid's state from Modified → Added so SaveChanges generates INSERT, not UPDATE.
        await _auctionRepo.UpdateAsync(auction, ct);
        await _auctionRepo.AddBidAsync(bid, ct);
        await _auctionRepo.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new BidPlacedEvent(auction.Id, bid.Id, cmd.BidderId, cmd.Amount), ct);

        if (previousWinnerId.HasValue && previousWinnerId != cmd.BidderId)
            await _eventBus.PublishAsync(new OutbidEvent(auction.Id, previousWinnerId.Value, 0, cmd.Amount), ct);

        // SignalR: push bid update real-time đến tất cả client đang xem auction này
        await _notifier.NotifyNewBid(auction.Id, cmd.BidderId, cmd.Amount, ct);

        return new PlaceBidResult(bid.Id, auction.CurrentPrice, auction.EndTime);
    }
}

// Interface định nghĩa ngay trong Application layer - Infrastructure implement
// Dev mode: InMemoryDistributedLock (không cần Redis)
// Production: nên thay bằng Redis-backed lock khi scale horizontally
public interface IDistributedLock
{
    Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan timeout);
}
