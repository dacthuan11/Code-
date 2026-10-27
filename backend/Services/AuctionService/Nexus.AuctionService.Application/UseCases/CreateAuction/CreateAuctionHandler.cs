using MediatR;
using Nexus.AuctionService.Domain.Entities;
using Nexus.AuctionService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.AuctionService.Application.UseCases.CreateAuction;

// Handler thực thi logic tạo auction mới
// Thứ tự: validate (by MediatR pipeline) → tạo entity → persist → publish event
public class CreateAuctionHandler : IRequestHandler<CreateAuctionCommand, CreateAuctionResult>
{
    private readonly IAuctionRepository _auctions;
    private readonly IEventBus _eventBus; // publish event để các service khác biết có auction mới

    public CreateAuctionHandler(IAuctionRepository auctions, IEventBus eventBus)
    {
        _auctions = auctions;
        _eventBus = eventBus;
    }

    public async Task<CreateAuctionResult> Handle(CreateAuctionCommand cmd, CancellationToken ct)
    {
        if (cmd.StartTime >= cmd.EndTime)
            throw new ArgumentException("End time must be after start time");

        var auction = Auction.Create(
            cmd.ProductId, cmd.SellerId, cmd.SellerEmail,
            cmd.StartingPrice, cmd.BidIncrement,
            cmd.StartTime, cmd.EndTime);

        // Tự động activate nếu startTime đã qua (hoặc trong vòng 1 phút nữa)
        if (cmd.StartTime <= DateTime.UtcNow.AddMinutes(1))
            auction.Activate();

        await _auctions.AddAsync(auction, ct);
        await _auctions.SaveChangesAsync(ct); // persist trước khi publish event

        // Publish event SAU khi DB commit thành công - tránh gửi event khi DB fail
        await _eventBus.PublishAsync(new AuctionCreatedEvent(
            auction.Id, auction.SellerId, auction.ProductId, auction.StartingPrice), ct);

        // Trả title từ command vì entity không lưu title (domain entity gọn, không lưu metadata thừa)
        return new CreateAuctionResult(auction.Id, cmd.Title, auction.StartingPrice, auction.EndTime);
    }
}
