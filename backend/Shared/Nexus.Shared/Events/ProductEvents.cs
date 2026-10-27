using Nexus.Shared.Messaging;

namespace Nexus.Shared.Events;

// ===== Product & Category Domain Events =====
// Publish từ CatalogService khi có thay đổi sản phẩm/danh mục
// CommerceService và AuctionService cần lắng nghe để sync dữ liệu

// Seller tạo sản phẩm mới — có thể trigger thông báo cho follower của seller
public record ProductCreatedEvent(Guid ProductId, Guid SellerId, string Name, decimal Price) : BaseIntegrationEvent;

// Cập nhật giá/tên — cart đang chứa sản phẩm này cần được cảnh báo
public record ProductUpdatedEvent(Guid ProductId, string Name, decimal Price) : BaseIntegrationEvent;

// Xóa sản phẩm — cần check xem có đang trong giỏ hàng hay auction không
public record ProductDeletedEvent(Guid ProductId) : BaseIntegrationEvent;

// Trạng thái: ACTIVE, INACTIVE, SUSPENDED...
public record ProductStatusChangedEvent(Guid ProductId, string OldStatus, string NewStatus) : BaseIntegrationEvent;

// Admin tạo danh mục — frontend cần refresh danh sách category
public record CategoryCreatedEvent(Guid CategoryId, string Name) : BaseIntegrationEvent;
public record CategoryUpdatedEvent(Guid CategoryId, string Name) : BaseIntegrationEvent;
public record CategoryDeletedEvent(Guid CategoryId) : BaseIntegrationEvent;

// SKU events — FulfillmentService lắng nghe để tạo/xóa InventoryRecord
public record SkuAddedEvent(Guid ProductId, Guid SkuId, string SkuCode, Guid SellerId) : BaseIntegrationEvent;
public record SkuRemovedEvent(Guid ProductId, Guid SkuId) : BaseIntegrationEvent;

// Review events
public record ProductReviewedEvent(Guid ProductId, Guid SellerId, Guid ReviewerId, int Rating, string Comment = "") : BaseIntegrationEvent;
