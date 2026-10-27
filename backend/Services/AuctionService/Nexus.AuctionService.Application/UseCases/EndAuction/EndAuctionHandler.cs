using MediatR;
using Nexus.AuctionService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.AuctionService.Application.UseCases.EndAuction;

// Handler kết thúc auction và publish kết quả ra các service khác
// OrderService lắng nghe AuctionWonEvent để tạo order, NotificationService báo kết quả cho user
public class EndAuctionHandler : IRequestHandler<EndAuctionCommand, EndAuctionResult>
{
    private readonly IAuctionRepository _auctions;
    private readonly IEventBus _eventBus;

    public EndAuctionHandler(IAuctionRepository auctions, IEventBus eventBus)
    {
        _auctions = auctions;
        _eventBus = eventBus;
    }

    public async Task<EndAuctionResult> Handle(EndAuctionCommand cmd, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(cmd.AuctionId, ct)
            ?? throw new KeyNotFoundException($"Auction {cmd.AuctionId} not found");

        // Gọi domain method - sẽ set Status = Ended nếu có winner, Failed nếu không có bid
        auction.End();
        // IAuctionRepository không có UpdateAsync - EF Core track thay đổi qua DbContext
        // Sẽ save ở SaveChangesAsync bên dưới

        // Lấy bid cao nhất từ Bids list (không có GetTopBid() method trong entity)
        // OrderByDescending để đảm bảo lấy đúng bid cao nhất, không phải bid mới nhất
        var topBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();

        // Auction entity không có ReservePrice - chỉ cần check có winner không
        // reserveMet = true khi có winner VÀ có bid hợp lệ trong danh sách
        bool reserveMet = auction.CurrentWinnerId.HasValue && topBid is not null;

        if (reserveMet && topBid is not null)
        {
            await _eventBus.PublishAsync(new AuctionWonEvent(
                auction.Id, topBid.BidderId, auction.SellerId,
                auction.ProductId, topBid.Amount,
                WinnerEmail: topBid.BidderEmail,
                SellerEmail: auction.SellerEmail), ct);
        }
        else
        {
            await _eventBus.PublishAsync(new AuctionFailedEvent(
                auction.Id, "Reserve price not met or no bids",
                SellerId: auction.SellerId,
                SellerEmail: auction.SellerEmail), ct);
        }

        await _auctions.SaveChangesAsync(ct); // persist sau khi đã quyết định publish event nào

        return new EndAuctionResult(auction.Id, topBid?.BidderId, topBid?.Amount, reserveMet);
    }
}
