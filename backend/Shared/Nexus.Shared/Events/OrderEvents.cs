using Nexus.Shared.Messaging;

namespace Nexus.Shared.Events;

// ===== Cart & Order Domain Events =====
// Publish từ CommerceService theo flow: Cart → Checkout → Order → Payment → Fulfillment

// Giỏ hàng được tạo lần đầu khi user thêm item đầu tiên
public record CartCreatedEvent(Guid CartId, Guid UserId) : BaseIntegrationEvent;

// Mỗi khi thêm item vào giỏ — analytics có thể track conversion
public record CartItemAddedEvent(Guid CartId, Guid ProductId, int Quantity) : BaseIntegrationEvent;

// Giỏ hết hạn sau 24h không checkout — cần cleanup
public record CartExpiredEvent(Guid CartId, Guid UserId) : BaseIntegrationEvent;

// Bắt đầu checkout — trigger lock inventory tạm thời
public record CheckoutStartedEvent(Guid OrderId, Guid UserId, decimal TotalAmount) : BaseIntegrationEvent;

// Order tạo thành công — FulfillmentService bắt đầu reserve kho
public record OrderCreatedEvent(Guid OrderId, Guid UserId, Guid SellerId, decimal TotalAmount,
    string UserEmail = "") : BaseIntegrationEvent;

// Thanh toán thành công — FulfillmentService tạo shipment
public record OrderPaidEvent(Guid OrderId, Guid UserId, decimal Amount, string PaymentRef) : BaseIntegrationEvent;

// Thanh toán thất bại — release inventory, notify user
public record OrderPaymentFailedEvent(Guid OrderId, Guid UserId, string Reason) : BaseIntegrationEvent;

// Hủy đơn — có thể do user, seller hoặc hệ thống (timeout, out of stock)
public record OrderCancelledEvent(Guid OrderId, Guid UserId, string Reason, string UserEmail = "",
    Guid BuyerId = default, decimal RefundAmount = 0) : BaseIntegrationEvent;

// Giao hàng thành công — trigger đánh giá
public record OrderCompletedEvent(Guid OrderId, Guid UserId) : BaseIntegrationEvent;

// Yêu cầu hoàn tiền
public record OrderRefundRequestedEvent(Guid OrderId, Guid UserId, decimal Amount) : BaseIntegrationEvent;
public record OrderRefundCompletedEvent(Guid OrderId, decimal Amount) : BaseIntegrationEvent;

// Seller xác nhận đơn COD
public record OrderConfirmedEvent(Guid OrderId, Guid SellerId, string UserEmail = "") : BaseIntegrationEvent;

// Đóng gói xong, FulfillmentService tạo Shipment
public record OrderReadyToShipEvent(
    Guid OrderId, Guid SellerId,
    string RecipientName, string Phone, string AddressLine,
    string Province, string District, string Ward,
    int ToDistrictId, string ToWardCode,
    string UserEmail = ""
) : BaseIntegrationEvent;

// GHN giao xong
public record OrderDeliveredEvent(Guid OrderId, Guid BuyerId, string BuyerEmail = "") : BaseIntegrationEvent;

// Voucher đã áp dụng thành công
public record VoucherAppliedEvent(Guid OrderId, string VoucherCode, decimal DiscountAmount) : BaseIntegrationEvent;
