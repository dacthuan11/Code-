using MassTransit;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CommerceService.Application.Consumers;

public class AuctionWonConsumer : IConsumer<AuctionWonEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IEventBus _eventBus;

    public AuctionWonConsumer(IOrderRepository orders, IEventBus eventBus)
    {
        _orders = orders;
        _eventBus = eventBus;
    }

    public async Task Consume(ConsumeContext<AuctionWonEvent> context)
    {
        var evt = context.Message;

        var item = OrderItem.Create(Guid.Empty, evt.ProductId, $"Product:{evt.ProductId}", evt.FinalPrice, 1);
        var order = Order.Create(evt.WinnerId, evt.SellerId, "Pending - Winner to provide", 0, [item], evt.AuctionId);

        await _orders.AddAsync(order, context.CancellationToken);
        await _orders.SaveChangesAsync(context.CancellationToken);

        await _eventBus.PublishAsync(
            new OrderCreatedEvent(order.Id, order.UserId, evt.SellerId, order.TotalAmount),
            context.CancellationToken);
    }
}
