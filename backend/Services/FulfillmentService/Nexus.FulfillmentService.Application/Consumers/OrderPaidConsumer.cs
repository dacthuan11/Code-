using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexus.FulfillmentService.Application.UseCases.CommitInventory;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.FulfillmentService.Application.Consumers;

/// <summary>
/// Khi đơn hàng được thanh toán thành công → commit toàn bộ reservation của đơn đó.
/// Commit = hàng thực sự xuất kho, giảm TotalQuantity vĩnh viễn.
/// </summary>
public class OrderPaidConsumer : IConsumer<OrderPaidEvent>
{
    private readonly IInventoryRepository _inventory;
    private readonly IMediator _mediator;
    private readonly ILogger<OrderPaidConsumer> _logger;

    public OrderPaidConsumer(IInventoryRepository inventory, IMediator mediator, ILogger<OrderPaidConsumer> logger)
    {
        _inventory = inventory;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPaidEvent> context)
    {
        var evt = context.Message;
        var reference = $"Order:{evt.OrderId}";

        var reservations = await _inventory.GetActiveReservationsByReferenceAsync(reference, context.CancellationToken);
        if (reservations.Count == 0)
        {
            _logger.LogWarning("OrderPaid: no active reservations found for {Reference}", reference);
            return;
        }

        foreach (var res in reservations)
        {
            await _mediator.Send(new CommitInventoryCommand(evt.OrderId, res.SkuId), context.CancellationToken);
            _logger.LogInformation("Committed inventory SKU={SkuId} for order {OrderId}", res.SkuId, evt.OrderId);
        }
    }
}
