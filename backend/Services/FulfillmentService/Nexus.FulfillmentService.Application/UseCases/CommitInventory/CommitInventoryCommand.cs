using MediatR;

namespace Nexus.FulfillmentService.Application.UseCases.CommitInventory;

/// <summary>
/// Command để xác nhận xuất kho sau khi đơn hàng được thanh toán thành công.
/// Commit = chuyển từ "giữ chỗ" sang "thực sự đã xuất" - TotalQuantity giảm thật.
/// Thường được trigger bởi PaymentSucceeded event từ PaymentService.
/// </summary>
public record CommitInventoryCommand(Guid OrderId, Guid SkuId) : IRequest;
