using MediatR;

namespace Nexus.AuctionService.Application.UseCases.CreateAuction;

// Command (CQRS) - immutable record, không có side effect nếu chưa được Handle
// SellerId/SellerEmail sẽ được inject từ JWT claim trong controller
public record CreateAuctionCommand(
    Guid ProductId,
    string Title,
    string Description,
    decimal StartingPrice,
    decimal? ReservePrice,
    decimal BidIncrement,
    DateTime StartTime,
    DateTime EndTime,
    Guid SellerId = default,
    string SellerEmail = ""
) : IRequest<CreateAuctionResult>;

// Result trả về sau khi tạo thành công - chỉ trả những gì client cần
public record CreateAuctionResult(Guid AuctionId, string Title, decimal StartingPrice, DateTime EndTime);
