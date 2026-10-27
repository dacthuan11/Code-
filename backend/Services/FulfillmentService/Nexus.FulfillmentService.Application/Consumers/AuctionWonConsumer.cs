using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexus.FulfillmentService.Application.UseCases.ReserveInventory;
using Nexus.Shared.Events;

namespace Nexus.FulfillmentService.Application.Consumers;

/// <summary>
/// Khi có người thắng đấu giá → reserve inventory ngay cho winner.
/// Auction dùng ProductId làm SkuId (1:1 trong MVP), Quantity = 1 (đấu giá từng sản phẩm).
/// Timeout kéo dài 1440 phút (24h) thay vì 30 phút vì winner cần thời gian thanh toán.
/// </summary>
public class AuctionWonConsumer : IConsumer<AuctionWonEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuctionWonConsumer> _logger;

    public AuctionWonConsumer(IMediator mediator, ILogger<AuctionWonConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuctionWonEvent> context)
    {
        var evt = context.Message;

        // Dùng AuctionId làm OrderId trong command, ProductId làm SkuId
        // Reference sẽ là "Auction:{auctionId}" thay vì "Order:{...}"
        var cmd = new ReserveInventoryCommand(
            OrderId: evt.AuctionId,
            SkuId: evt.ProductId,
            Quantity: 1,
            CustomReference: $"Auction:{evt.AuctionId}",
            TimeoutMinutes: 1440 // 24h để winner kịp thanh toán
        );

        try
        {
            var result = await _mediator.Send(cmd, context.CancellationToken);
            _logger.LogInformation(
                "Auction {AuctionId} won by {WinnerId} - inventory reserved (ReservationId={ReservationId}, ExpiresAt={ExpiresAt})",
                evt.AuctionId, evt.WinnerId, result.ReservationId, result.ExpiresAt);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("AuctionWon: Product {ProductId} not found in inventory. {Message}", evt.ProductId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("AuctionWon: Cannot reserve inventory for product {ProductId}. {Message}", evt.ProductId, ex.Message);
        }
    }
}
