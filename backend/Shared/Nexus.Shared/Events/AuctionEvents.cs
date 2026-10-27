using Nexus.Shared.Messaging;

namespace Nexus.Shared.Events;

// ===== Auction Domain Events =====
// Publish từ AuctionService theo vòng đời: Created → Started → (Bids) → Ended/Cancelled → Won/Failed
// NotificationService subscribe để gửi thông báo real-time cho bidder

// Seller tạo phiên đấu giá mới (chưa bắt đầu)
public record AuctionCreatedEvent(Guid AuctionId, Guid SellerId, Guid ProductId, decimal StartingPrice) : BaseIntegrationEvent;

// AuctionService auto trigger khi đến StartTime (qua background service)
public record AuctionStartedEvent(Guid AuctionId, DateTime StartTime, DateTime EndTime) : BaseIntegrationEvent;

// Kết thúc đấu giá — sau đó check winner vs reserve price
public record AuctionEndedEvent(Guid AuctionId) : BaseIntegrationEvent;

// Admin hoặc seller hủy — trước khi có bid hoặc theo chính sách
public record AuctionCancelledEvent(Guid AuctionId, string CancelledBy, string Reason) : BaseIntegrationEvent;

// Có bid mới — cần notify tất cả bidder đang theo dõi
public record BidPlacedEvent(Guid AuctionId, Guid BidId, Guid BidderId, decimal BidAmount) : BaseIntegrationEvent;

// Bidder trước bị vượt qua — cần notify để họ có thể bid cao hơn
public record OutbidEvent(Guid AuctionId, Guid PreviousBidderId, decimal PreviousAmount, decimal NewAmount) : BaseIntegrationEvent;

// Đấu giá có winner — trigger tạo order và giao dịch thanh toán
public record AuctionWonEvent(Guid AuctionId, Guid WinnerId, Guid SellerId, Guid ProductId, decimal FinalPrice,
    string WinnerEmail = "", string SellerEmail = "") : BaseIntegrationEvent;

// Không có ai bid, hoặc giá cuối < reserve price — không có giao dịch
public record AuctionFailedEvent(Guid AuctionId, string Reason,
    Guid SellerId = default, string SellerEmail = "") : BaseIntegrationEvent;

// Winner không thanh toán trong 24h → phạt điểm uy tín
public record AuctionPaymentTimeoutEvent(Guid AuctionId, Guid WinnerId, decimal PenaltyScore) : BaseIntegrationEvent;

// Thanh toán xong, order được tạo — FulfillmentService bắt đầu xử lý
public record AuctionSettledEvent(Guid AuctionId, Guid OrderId, decimal FinalPrice) : BaseIntegrationEvent;
