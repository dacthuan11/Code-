using MediatR;
using Nexus.CommerceService.Domain.Repositories;

namespace Nexus.CommerceService.Application.UseCases.CompleteOrder;

public record CompleteOrderCommand(Guid OrderId) : IRequest<bool>;

public class CompleteOrderHandler : IRequestHandler<CompleteOrderCommand, bool>
{
    private readonly IOrderRepository _orders;

    public CompleteOrderHandler(IOrderRepository orders) => _orders = orders;

    public async Task<bool> Handle(CompleteOrderCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null || order.Status != Nexus.CommerceService.Domain.Entities.OrderStatus.Paid)
            return false;

        order.Complete();
        await _orders.UpdateAsync(order, ct);
        await _orders.SaveChangesAsync(ct);
        return true;
    }
}
