using MediatR;

namespace Nexus.FulfillmentService.Application.UseCases.ReserveInventory;

/// <summary>
/// Command để giữ chỗ hàng cho một đơn hàng.
/// Gửi lên khi user checkout - cần giữ hàng trước khi họ thanh toán
/// để tránh tình trạng nhiều người cùng mua một sản phẩm cuối cùng.
/// </summary>
public record ReserveInventoryCommand(
    Guid OrderId,
    Guid SkuId,
    int Quantity,
    string? CustomReference = null,  // override "Order:{OrderId}" - dùng cho auction: "Auction:{id}"
    int TimeoutMinutes = 30
) : IRequest<ReserveInventoryResult>;

/// <summary>
/// Kết quả trả về cho caller biết ReservationId và thời điểm hết hạn.
/// Caller (OrderService) nên lưu lại ExpiresAt để hiển thị countdown cho user.
/// </summary>
public record ReserveInventoryResult(Guid ReservationId, DateTime ExpiresAt);
