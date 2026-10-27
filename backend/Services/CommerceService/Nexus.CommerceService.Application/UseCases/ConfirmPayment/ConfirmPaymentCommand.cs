using MediatR;
using Nexus.CommerceService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CommerceService.Application.UseCases.ConfirmPayment;

public record ConfirmPaymentCommand(Guid OrderId, string PaymentRef) : IRequest<bool>;

public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, bool>
{
    private readonly IOrderRepository _orders;
    private readonly IEventBus _eventBus;

    public ConfirmPaymentHandler(IOrderRepository orders, IEventBus eventBus)
    {
        _orders = orders;
        _eventBus = eventBus;
    }

    public async Task<bool> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null || order.Status != Nexus.CommerceService.Domain.Entities.OrderStatus.PendingPayment)
            return false;

        order.MarkPaid(cmd.PaymentRef);
        await _orders.UpdateAsync(order, ct);
        await _orders.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new OrderPaidEvent(order.Id, order.UserId, order.TotalAmount, cmd.PaymentRef), ct);

        return true;
    }
}
