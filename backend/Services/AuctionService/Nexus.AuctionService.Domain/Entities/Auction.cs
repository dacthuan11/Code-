using Nexus.Shared.Domain;

namespace Nexus.AuctionService.Domain.Entities;

// Trạng thái của phiên đấu giá - theo vòng đời từ Draft đến Settled/Failed
// Draft → Scheduled → Active → Ended/Failed → Settled
public enum AuctionStatus { Draft, Scheduled, Active, Ended, Settled, Cancelled, Failed }

// Entity gốc của AuctionService - tất cả business rules đều nằm đây
// Dùng private setters để đảm bảo chỉ domain methods mới thay đổi state (DDD aggregate root)
public class Auction : BaseEntity
{
    public Guid ProductId { get; private set; }         // sản phẩm đang được đấu giá
    public Guid SellerId { get; private set; }          // người bán tạo auction
    public string SellerEmail { get; private set; } = string.Empty;
    public decimal StartingPrice { get; private set; }  // giá khởi điểm
    public decimal BidIncrement { get; private set; }   // bước giá tối thiểu mỗi lần bid
    public decimal CurrentPrice { get; private set; }   // giá hiện tại (cập nhật mỗi bid thành công)
    public Guid? CurrentWinnerId { get; private set; }  // null = chưa có ai bid
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Draft;

    // Deadline 24h để winner thanh toán — null nếu chưa có winner hoặc đã thanh toán
    public DateTime? PaymentDeadline { get; private set; }

    // Đếm số lần đã gia hạn do anti-sniping - không cho gia hạn vô tận
    public int AntiSnipingExtensions { get; private set; }
    public List<Bid> Bids { get; private set; } = new();

    // Giới hạn anti-sniping: tối đa 3 lần, mỗi lần +5 phút
    private const int MaxAntiSnipingExtensions = 3;
    private const int AntiSnipingWindowMinutes = 5;

    // EF Core cần constructor không tham số để reconstruct entity từ DB
    private Auction() { }

    // Factory method thay vì constructor public - dễ đọc hơn và có thể add validation logic sau
    public static Auction Create(Guid productId, Guid sellerId, string sellerEmail,
        decimal startingPrice, decimal bidIncrement,
        DateTime startTime, DateTime endTime) => new()
    {
        ProductId = productId, SellerId = sellerId, SellerEmail = sellerEmail,
        StartingPrice = startingPrice, CurrentPrice = startingPrice,
        BidIncrement = bidIncrement,
        StartTime = startTime, EndTime = endTime
    };

    // Các method đơn giản để chuyển trạng thái - Touch() cập nhật UpdatedAt trong BaseEntity
    public void Activate() { Status = AuctionStatus.Active; Touch(); }
    public void Cancel(string reason) { Status = AuctionStatus.Cancelled; Touch(); }
    public void Fail() { Status = AuctionStatus.Failed; Touch(); }
    public void Settle(Guid orderId) { Status = AuctionStatus.Settled; Touch(); }

    // Business logic chính - đặt bid và áp dụng anti-sniping nếu cần
    // Throws nếu vi phạm business rule, caller phải catch và handle
    public Bid PlaceBid(Guid bidderId, string bidderEmail, decimal amount)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is not active.");

        // Bid phải cao hơn giá hiện tại ÍT NHẤT 1 bước giá (bidIncrement)
        if (amount < CurrentPrice + BidIncrement)
            throw new InvalidOperationException($"Bid must be at least {CurrentPrice + BidIncrement}.");

        // Người bán không được bid vào auction của chính mình - tránh wash trading
        if (bidderId == SellerId)
            throw new InvalidOperationException("Seller cannot bid on own auction.");

        var previousWinnerId = CurrentWinnerId; // lưu lại để handler biết ai bị outbid
        CurrentWinnerId = bidderId;
        CurrentPrice = amount;

        // Anti-sniping: nếu bid vào trong 5 phút cuối thì gia hạn thêm 5 phút
        // Mục đích: tránh việc sniper bid vào giây cuối để win mà không cho người khác phản ứng
        // Tối đa 3 lần gia hạn để tránh auction kéo dài vô tận
        if (DateTime.UtcNow > EndTime.AddMinutes(-AntiSnipingWindowMinutes)
            && AntiSnipingExtensions < MaxAntiSnipingExtensions)
        {
            EndTime = EndTime.AddMinutes(AntiSnipingWindowMinutes);
            AntiSnipingExtensions++;
        }

        var bid = Bid.Create(Id, bidderId, bidderEmail, amount);
        Bids.Add(bid);
        Touch(); // cập nhật UpdatedAt
        return bid; // trả về bid để handler có Id cho response
    }

    // Kết thúc auction: nếu có winner thì Ended + set PaymentDeadline 24h, không có bid thì Failed
    public void End()
    {
        if (CurrentWinnerId.HasValue)
        {
            Status = AuctionStatus.Ended;
            PaymentDeadline = DateTime.UtcNow.AddHours(24);
        }
        else
        {
            Status = AuctionStatus.Failed;
        }
        Touch();
    }

    // Gọi khi winner đã thanh toán — xoá deadline để job không phạt nữa
    public void ClearPaymentDeadline() { PaymentDeadline = null; Touch(); }
}

// Value object đơn giản - lưu lịch sử từng lần bid
// Không cần nhiều business logic ở đây, logic nằm trong Auction.PlaceBid
public class Bid : BaseEntity
{
    public Guid AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public string BidderEmail { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }

    private Bid() { } // EF Core cần constructor rỗng

    public static Bid Create(Guid auctionId, Guid bidderId, string bidderEmail, decimal amount)
        => new() { AuctionId = auctionId, BidderId = bidderId, BidderEmail = bidderEmail, Amount = amount };
}
