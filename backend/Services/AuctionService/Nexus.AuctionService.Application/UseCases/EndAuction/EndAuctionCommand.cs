using MediatR;

namespace Nexus.AuctionService.Application.UseCases.EndAuction;

// Command kết thúc auction - thường được gọi bởi scheduler/background job khi hết giờ
// Hoặc admin force-end thủ công qua API endpoint POST /auctions/{id}/end
public record EndAuctionCommand(Guid AuctionId) : IRequest<EndAuctionResult>;

// Kết quả: có winner thì WinnerId/WinningBid có giá trị, ReserveMet = true
// Không có bid hoặc không đạt reserve thì WinnerId = null, ReserveMet = false
public record EndAuctionResult(Guid AuctionId, Guid? WinnerId, decimal? WinningBid, bool ReserveMet);
