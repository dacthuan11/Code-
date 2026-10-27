using Nexus.Shared.Domain;

namespace Nexus.CommerceService.Domain.Entities;

// ── Vòng đời đơn hàng (chuẩn theo Shopee/Tiki/Lazada) ──────────────────────
// Happy path:
//   PendingPayment → Confirmed → Processing → ReadyToShip → Shipped
//   → OutForDelivery → Delivered → Completed
// Hủy: PendingPayment/Confirmed → Cancelled
// Hoàn: Delivered/Completed → RefundRequested → Refunded
// Pre-order quốc tế: PreOrder → (sau khi hàng về kho) → Confirmed → ...
//
// Giá trị int được giữ cố định để backward-compat với dữ liệu đã có trong DB.
public enum OrderStatus
{
    PendingPayment  = 0,   // Chờ thanh toán / chờ xác nhận (COD)
    Paid            = 1,   // [Legacy] đã thanh toán online (→ dùng Confirmed thay)
    Cancelled       = 2,   // Đã hủy
    Completed       = 3,   // Hoàn thành (sau delivered, hết thời gian khiếu nại)
    Refunded        = 4,   // Đã hoàn tiền
    Confirmed       = 5,   // Seller xác nhận đơn
    Processing      = 6,   // Đang chuẩn bị / đóng gói hàng
    ReadyToShip     = 7,   // Đã đóng gói, chờ GHN đến lấy
    Shipped         = 8,   // GHN đã lấy hàng
    OutForDelivery  = 9,   // Đang giao (shipper mang đến)
    Delivered       = 10,  // Đã giao thành công
    RefundRequested = 11,  // Yêu cầu hoàn tiền đang chờ duyệt
    PreOrder        = 12,  // Hàng quốc tế / cần đặt trước
}

// ── Loại giao hàng ──────────────────────────────────────────────────────────
public enum DeliveryType
{
    Standard = 0,   // Giao hàng tiêu chuẩn (3-5 ngày nội tỉnh, 5-7 ngày liên tỉnh)
    Express  = 1,   // Hỏa tốc (same-day / next-day, trong bán kính 20km)
    PreOrder = 2,   // Hàng đặt trước / nhập khẩu (7-21 ngày)
}

// ── Lý do hủy đơn (chuẩn theo GHN + thực tế e-commerce VN) ────────────────
public static class CancellationReasons
{
    // Phía người mua
    public const string BuyerChanged = "Người mua muốn thay đổi sản phẩm";
    public const string BuyerAddressChange = "Người mua muốn thay đổi địa chỉ giao";
    public const string BuyerFoundCheaper = "Người mua tìm được giá rẻ hơn";
    public const string BuyerNoLongerNeeded = "Người mua không còn nhu cầu";
    public const string BuyerLongWaitTime = "Thời gian giao hàng quá lâu";
    public const string BuyerRefuse = "Người mua từ chối nhận hàng";

    // Phía người bán
    public const string SellerOutOfStock = "Hàng hóa hết hàng";
    public const string SellerCannotDeliver = "Người bán không thể giao trong thời gian yêu cầu";
    public const string SellerFraudSuspect = "Nghi ngờ đơn hàng giả mạo";

    // Phía vận chuyển (GHN)
    public const string AddressNotFound = "Địa chỉ không tìm thấy";
    public const string RecipientUnreachable = "Không liên lạc được người nhận";
    public const string PackageDamaged = "Hàng hư hỏng trong quá trình vận chuyển";
    public const string ReturnByCarrier = "Đơn vị vận chuyển hoàn hàng";

    public static readonly string[] AllReasons = [
        BuyerChanged, BuyerAddressChange, BuyerFoundCheaper,
        BuyerNoLongerNeeded, BuyerLongWaitTime, BuyerRefuse,
        SellerOutOfStock, SellerCannotDeliver, SellerFraudSuspect,
        AddressNotFound, RecipientUnreachable, PackageDamaged, ReturnByCarrier,
    ];
}

// ── Lịch sử thay đổi trạng thái đơn hàng ────────────────────────────────────
public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Note { get; private set; }    // lý do / ghi chú (VD: lý do hủy)
    public string? Actor { get; private set; }   // "system" | "seller" | "buyer" | email

    private OrderStatusHistory() { }

    public static OrderStatusHistory Create(Guid orderId, OrderStatus status, string? note = null, string? actor = null)
        => new() { OrderId = orderId, Status = status, Note = note, Actor = actor ?? "system" };
}

// ── Order aggregate ──────────────────────────────────────────────────────────
public class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid SellerId { get; private set; }

    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;
    public DeliveryType DeliveryType { get; private set; } = DeliveryType.Standard;

    public decimal TotalAmount { get; private set; }
    public decimal ShippingFee { get; private set; }
    public string ShippingAddress { get; private set; } = default!;

    // Null cho đến khi payment gateway confirm
    public string? PaymentRef { get; private set; }

    // Thời gian dự kiến giao hàng (từ GHN hoặc tính toán)
    public DateTime? EstimatedDelivery { get; private set; }

    // Lý do hủy đơn (set khi Cancel())
    public string? CancellationReason { get; private set; }

    // Nếu order từ đấu giá
    public Guid? AuctionId { get; private set; }

    public List<OrderItem> Items { get; private set; } = new();
    public List<OrderStatusHistory> StatusHistory { get; private set; } = new();

    private Order() { }

    private void RecordHistory(OrderStatus status, string? note = null, string? actor = null)
        => StatusHistory.Add(OrderStatusHistory.Create(Id, status, note, actor));

    public static Order Create(Guid userId, Guid sellerId, string shippingAddress,
        decimal shippingFee, List<OrderItem> items, Guid? auctionId = null,
        DeliveryType deliveryType = DeliveryType.Standard,
        DateTime? estimatedDelivery = null)
    {
        var total = items.Sum(i => i.UnitPrice * i.Quantity) + shippingFee;
        var order = new Order
        {
            UserId = userId, SellerId = sellerId,
            ShippingAddress = shippingAddress, ShippingFee = shippingFee,
            TotalAmount = total, Items = items, AuctionId = auctionId,
            DeliveryType = deliveryType,
            EstimatedDelivery = estimatedDelivery ?? CalculateDefaultEstimate(deliveryType),
        };
        order.RecordHistory(auctionId.HasValue ? OrderStatus.PendingPayment : OrderStatus.PendingPayment, "Đơn hàng được tạo", "buyer");
        return order;
    }

    // Tính thời gian giao hàng mặc định dựa trên loại dịch vụ
    private static DateTime CalculateDefaultEstimate(DeliveryType type) => type switch
    {
        DeliveryType.Express  => DateTime.UtcNow.AddHours(4),          // hỏa tốc: trong ngày
        DeliveryType.PreOrder => DateTime.UtcNow.AddDays(14),          // nhập khẩu: ~14 ngày
        _                     => DateTime.UtcNow.AddDays(4),           // tiêu chuẩn: ~4 ngày
    };

    // ── Transitions ──────────────────────────────────────────────────────────

    // Payment gateway confirm → Confirmed (chuẩn) hoặc Paid (legacy)
    public void MarkPaid(string paymentRef)
    {
        PaymentRef = paymentRef;
        Status = OrderStatus.Confirmed;
        RecordHistory(OrderStatus.Confirmed, "Thanh toán online thành công", "payment-gateway");
        Touch();
    }

    // COD: seller tay xác nhận
    public void Confirm()
    {
        if (Status != OrderStatus.PendingPayment && Status != OrderStatus.PreOrder) return;
        Status = OrderStatus.Confirmed;
        RecordHistory(OrderStatus.Confirmed, "Seller xác nhận đơn", "seller");
        Touch();
    }

    // Seller đang chuẩn bị hàng
    public void StartProcessing()
    {
        if (Status != OrderStatus.Confirmed) return;
        Status = OrderStatus.Processing;
        RecordHistory(OrderStatus.Processing, "Đang chuẩn bị và đóng gói hàng", "seller");
        Touch();
    }

    // Đóng gói xong, chờ GHN đến lấy
    public void MarkReadyToShip()
    {
        if (Status != OrderStatus.Processing) return;
        Status = OrderStatus.ReadyToShip;
        RecordHistory(OrderStatus.ReadyToShip, "Hàng đã đóng gói, chờ shipper đến lấy", "seller");
        Touch();
    }

    // GHN đã lấy hàng
    public void MarkShipped(string? trackingNumber = null, DateTime? estimatedDelivery = null)
    {
        Status = OrderStatus.Shipped;
        if (estimatedDelivery.HasValue) EstimatedDelivery = estimatedDelivery;
        RecordHistory(OrderStatus.Shipped, trackingNumber != null ? $"Shipper đã lấy hàng — mã vận đơn: {trackingNumber}" : "Shipper đã lấy hàng", "carrier");
        Touch();
    }

    // Đang giao đến người nhận
    public void MarkOutForDelivery()
    {
        Status = OrderStatus.OutForDelivery;
        RecordHistory(OrderStatus.OutForDelivery, "Đang trên đường giao đến bạn", "carrier");
        Touch();
    }

    // Giao thành công
    public void MarkDelivered()
    {
        Status = OrderStatus.Delivered;
        RecordHistory(OrderStatus.Delivered, "Giao hàng thành công", "carrier");
        Touch();
    }

    // Hủy đơn (có lý do)
    public void Cancel(string? reason = null, string? actor = null)
    {
        CancellationReason = reason;
        Status = OrderStatus.Cancelled;
        RecordHistory(OrderStatus.Cancelled, reason ?? "Đơn hàng bị hủy", actor ?? "system");
        Touch();
    }

    // Đơn hoàn thành (hết thời gian khiếu nại sau Delivered)
    public void Complete()
    {
        Status = OrderStatus.Completed;
        RecordHistory(OrderStatus.Completed, "Đơn hàng hoàn thành", "system");
        Touch();
    }

    // Yêu cầu hoàn tiền
    public void RequestRefund()
    {
        Status = OrderStatus.RefundRequested;
        RecordHistory(OrderStatus.RefundRequested, "Yêu cầu hoàn tiền đang được xử lý", "buyer");
        Touch();
    }

    // Hoàn tiền xong
    public void Refund()
    {
        Status = OrderStatus.Refunded;
        RecordHistory(OrderStatus.Refunded, "Hoàn tiền thành công", "system");
        Touch();
    }

    // Cập nhật thời gian giao hàng dự kiến (từ GHN response)
    public void SetEstimatedDelivery(DateTime estimate)
    {
        EstimatedDelivery = estimate;
        Touch();
    }
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, Guid productId, string productName, decimal unitPrice, int qty)
        => new() { OrderId = orderId, ProductId = productId, ProductName = productName, UnitPrice = unitPrice, Quantity = qty };
}
