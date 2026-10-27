using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.FulfillmentService.Application.UseCases.ReleaseInventory;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.BackgroundJobs;

/// <summary>
/// Background job chạy mỗi 60 giây để tìm và giải phóng các reservation đã hết hạn.
/// Cần thiết để tránh "ghost reservation" treo stock mãi mãi khi user bỏ ngang checkout.
/// Dùng IServiceScopeFactory vì IHostedService là singleton, còn repository là scoped.
/// </summary>
public class ReservationExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public ReservationExpiryJob(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpiryJob started - checking every {Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing expired reservations");
            }
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var expired = await inventory.GetExpiredActiveReservationsAsync(ct);
        if (expired.Count == 0) return;

        _logger.LogInformation("Found {Count} expired reservations to release", expired.Count);

        foreach (var res in expired)
        {
            // Parse OrderId từ Reference (format "Order:{guid}" hoặc "Auction:{guid}")
            var parts = res.Reference.Split(':');
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var refId))
            {
                _logger.LogWarning("Cannot parse reference '{Reference}' for reservation {Id}", res.Reference, res.Id);
                continue;
            }

            await mediator.Send(new ReleaseInventoryCommand(refId, res.SkuId, "Reservation expired (timeout 30m)"), ct);
            await eventBus.PublishAsync(new InventoryReservationExpiredEvent(res.Id), ct);
            _logger.LogInformation("Released expired reservation {Id} for {Reference}", res.Id, res.Reference);
        }
    }
}
