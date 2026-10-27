using MediatR;

namespace Nexus.AuctionService.Application.UseCases.PlaceBid;

// Command đặt bid - BidderId lấy từ JWT (controller inject), không nhận từ body
// Tách riêng PlaceBidRequest ở controller level để không expose BidderId cho client tự set
public record PlaceBidCommand(Guid AuctionId, Guid BidderId, string BidderEmail, decimal Amount) : IRequest<PlaceBidResult>;

// Trả về thông tin sau khi bid thành công
// NewEndTime quan trọng vì có thể thay đổi so với ban đầu do anti-sniping gia hạn
public record PlaceBidResult(Guid BidId, decimal CurrentPrice, DateTime NewEndTime);
