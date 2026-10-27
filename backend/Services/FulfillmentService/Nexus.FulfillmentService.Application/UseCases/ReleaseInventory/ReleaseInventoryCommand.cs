using MediatR;

namespace Nexus.FulfillmentService.Application.UseCases.ReleaseInventory;

/// <summary>
/// Command để giải phóng hàng đang được giữ chỗ - thường xảy ra khi:
/// - User hủy đơn hàng
/// - Reservation timeout (quá 30 phút chưa thanh toán)
/// - Payment thất bại
/// Reason string để ghi vào ledger, giúp audit sau này.
/// </summary>
public record ReleaseInventoryCommand(Guid OrderId, Guid SkuId, string Reason) : IRequest;
