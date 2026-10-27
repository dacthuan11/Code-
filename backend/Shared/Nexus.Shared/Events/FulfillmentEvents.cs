using Nexus.Shared.Messaging;

namespace Nexus.Shared.Events;

// ===== Inventory & Fulfillment Events =====
// Publish từ FulfillmentService khi có thay đổi về kho và vận chuyển

// Kho được giữ chỗ cho đơn hàng — tạm thời khóa số lượng này
// Reference thường là "Order:{orderId}" để trace ngược về đơn hàng
public record InventoryReservedEvent(Guid ReservationId, Guid SkuId, int Quantity, string Reference) : BaseIntegrationEvent;

// Giải phóng kho — khi đơn bị hủy hoặc hết thời gian giữ chỗ
public record InventoryReleasedEvent(Guid ReservationId, Guid SkuId, int Quantity) : BaseIntegrationEvent;

// Commit kho — khi đơn được xác nhận giao hàng, trừ vĩnh viễn khỏi kho
public record InventoryCommittedEvent(Guid OrderId, Guid SkuId, int Quantity) : BaseIntegrationEvent;

// Reserve hết hạn mà không commit — cần release và notify
public record InventoryReservationExpiredEvent(Guid ReservationId) : BaseIntegrationEvent;

// Điều chỉnh kho thủ công — nhập thêm hàng, kiểm kê thực tế...
public record InventoryAdjustedEvent(Guid SkuId, int Delta, string Reason) : BaseIntegrationEvent;

// Vận đơn được tạo và giao cho đơn vị vận chuyển
public record ShipmentCreatedEvent(Guid ShipmentId, Guid OrderId, string TrackingNumber, string Carrier, Guid BuyerId = default, string BuyerEmail = "", string RecipientName = "", DateTime? ExpectedDelivery = null) : BaseIntegrationEvent;

// Đơn vị vận chuyển đã lấy hàng
public record ShipmentPickedUpEvent(Guid ShipmentId, string TrackingNumber) : BaseIntegrationEvent;

// Đang trên đường giao
public record ShipmentInTransitEvent(Guid ShipmentId, string TrackingNumber) : BaseIntegrationEvent;

// Giao thành công — trigger OrderCompleted và đánh giá
public record ShipmentDeliveredEvent(Guid ShipmentId, Guid OrderId, Guid BuyerId = default, string TrackingNumber = "", string BuyerEmail = "", string RecipientName = "") : BaseIntegrationEvent;

// Giao thất bại — cần retry hoặc hoàn hàng
public record ShipmentFailedEvent(Guid ShipmentId, string Reason) : BaseIntegrationEvent;

// Hàng bị hoàn về — cần nhập lại kho
public record ShipmentReturnedEvent(Guid ShipmentId, Guid OrderId) : BaseIntegrationEvent;

// Return/Refund flow
public record ReturnRequestCreatedEvent(Guid ReturnId, Guid OrderId, Guid SellerId, string BuyerEmail = "") : BaseIntegrationEvent;
public record ReturnApprovedEvent(Guid ReturnId, Guid OrderId, Guid BuyerId, decimal RefundAmount, string BuyerEmail = "") : BaseIntegrationEvent;
public record ReturnRejectedEvent(Guid ReturnId, Guid OrderId, Guid BuyerId, string SellerNote = "") : BaseIntegrationEvent;
