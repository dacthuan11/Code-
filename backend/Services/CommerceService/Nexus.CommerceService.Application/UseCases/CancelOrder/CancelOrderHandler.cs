using MediatR;
using Nexus.CommerceService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CommerceService.Application.UseCases.CancelOrder;

// Handler huỷ đơn hàng.
// Logic đơn giản hơn Checkout nhưng cần đảm bảo 2 điều:
//   1. User chỉ huỷ được đơn của chính mình (authorization check trong domain handler)
//   2. Publish event để các service khác biết (ví dụ: InventoryService hoàn lại stock)
public class CancelOrderHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orders;
    private readonly IEventBus _eventBus;

    public CancelOrderHandler(IOrderRepository orders, IEventBus eventBus)
    {
        _orders = orders;
        _eventBus = eventBus;
    }

    public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"Order {cmd.OrderId} not found");

        // Authorization check: user chỉ được huỷ đơn của chính mình
        // Làm ở đây thay vì ở controller vì đây là business rule, không phải infrastructure concern
        if (order.UserId != cmd.RequestedByUserId)
            throw new UnauthorizedAccessException("Cannot cancel another user's order");

        // Order có thể hủy khi chưa shipped
        if (order.Status >= Nexus.CommerceService.Domain.Entities.OrderStatus.Shipped)
            throw new InvalidOperationException("Không thể hủy đơn hàng đã giao cho vận chuyển.");

        order.Cancel(cmd.Reason, cmd.UserEmail);

        await _orders.UpdateAsync(order, ct);
        await _orders.SaveChangesAsync(ct);

        // Publish event với reason để audit log và các consumer downstream xử lý
        // ví dụ: InventoryService nghe event này để hoàn lại stock
        await _eventBus.PublishAsync(new OrderCancelledEvent(order.Id, order.UserId, cmd.Reason, cmd.UserEmail), ct);
    }
}
