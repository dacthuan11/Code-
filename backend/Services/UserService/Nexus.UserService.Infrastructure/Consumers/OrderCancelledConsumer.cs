using MassTransit;
using MediatR;
using Nexus.Shared.Events;
using Nexus.UserService.Application.UseCases.ManageWallet;

namespace Nexus.UserService.Infrastructure.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly IMediator _mediator;
    public OrderCancelledConsumer(IMediator mediator) => _mediator = mediator;

    public async Task Consume(ConsumeContext<OrderCancelledEvent> ctx)
    {
        var msg = ctx.Message;
        if (msg.RefundAmount > 0 && msg.BuyerId != Guid.Empty)
        {
            await _mediator.Send(new CreditWalletCommand(
                msg.BuyerId,
                msg.RefundAmount,
                $"Hoàn tiền đơn hàng #{msg.OrderId.ToString()[..8]}",
                $"ORDER_CANCEL_{msg.OrderId}"));
        }
    }
}
