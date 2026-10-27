using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexus.FulfillmentService.Application.UseCases.ReleaseInventory;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.FulfillmentService.Application.Consumers;

/// <summary>
/// Khi đơn hàng bị hủy → giải phóng toàn bộ reservation để trả lại stock.
/// Release chỉ bỏ "giữ chỗ", không giảm TotalQuantity (hàng vẫn ở kho).
/// </summary>
public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly IInventoryRepository _inventory;
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(IInventoryRepository inventory, IMediator mediator, ILogger<OrderCancelledConsumer> logger)
    {
        _inventory = inventory;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var evt = context.Message;
        var reference = $"Order:{evt.OrderId}";

        var reservations = await _inventory.GetActiveReservationsByReferenceAsync(reference, context.CancellationToken);
        if (reservations.Count == 0)
        {
            _logger.LogInformation("OrderCancelled: no active reservations for {Reference} (already released or committed)", reference);
            return;
        }

        foreach (var res in reservations)
        {
            await _mediator.Send(new ReleaseInventoryCommand(evt.OrderId, res.SkuId, $"Order cancelled: {evt.Reason}"), context.CancellationToken);
            _logger.LogInformation("Released inventory SKU={SkuId} for cancelled order {OrderId}", res.SkuId, evt.OrderId);
        }
    }
}
