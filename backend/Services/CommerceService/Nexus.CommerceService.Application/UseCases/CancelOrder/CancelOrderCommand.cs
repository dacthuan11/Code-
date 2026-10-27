using MediatR;

namespace Nexus.CommerceService.Application.UseCases.CancelOrder;

// Command huỷ đơn hàng - yêu cầu cả OrderId lẫn RequestedByUserId
// để handler có thể verify authorization (chỉ owner mới huỷ được)
// Không dùng IRequest<T> (không trả về kết quả) vì cancel thành công thì không cần trả về gì cả
public record CancelOrderCommand(Guid OrderId, Guid RequestedByUserId, string Reason, string UserEmail = "") : IRequest;
