using MediatR;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.UseCases.ReleaseInventory;

/// <summary>
/// Xử lý Release Inventory - trả lại hàng về available pool.
/// Logic tương tự CommitInventoryHandler nhưng khác ở chỗ:
/// - Release: hoàn hàng về, stock available tăng lại
/// - Commit: hàng ra kho thật, stock total giảm
/// </summary>
public class ReleaseInventoryHandler : IRequestHandler<ReleaseInventoryCommand>
{
    private readonly IInventoryRepository _inventory;
    private readonly IEventBus _eventBus;

    public ReleaseInventoryHandler(IInventoryRepository inventory, IEventBus eventBus)
    {
        _inventory = inventory;
        _eventBus = eventBus;
    }

    public async Task Handle(ReleaseInventoryCommand cmd, CancellationToken ct)
    {
        // Nếu không tìm thấy reservation: có thể đã released trước đó (idempotency concern)
        // Hiện tại throw exception, sau này có thể đổi thành no-op nếu cần idempotent
        var reservation = await _inventory.GetReservationByOrderAsync(cmd.OrderId, cmd.SkuId, ct)
            ?? throw new KeyNotFoundException($"No reservation for order {cmd.OrderId} / SKU {cmd.SkuId}");

        var record = await _inventory.GetBySkuAsync(cmd.SkuId, ct)
            ?? throw new KeyNotFoundException($"SKU {cmd.SkuId} not found");

        // Domain model dùng Release(int qty) + MarkReleased() riêng
        // Release chỉ giảm ReservedQuantity, không đụng TotalQuantity
        record.Release(reservation.Quantity);
        reservation.MarkReleased();

        await _inventory.UpdateReservationAsync(reservation, ct);

        // Delta dương vì hàng quay về pool available (released back to stock)
        await _inventory.AppendLedgerAsync(cmd.SkuId, "RELEASE", reservation.Quantity, $"Released: {cmd.Reason}", ct);
        await _inventory.SaveChangesAsync(ct);

        // Publish event để OrderService biết mà cập nhật trạng thái đơn hàng
        await _eventBus.PublishAsync(new InventoryReleasedEvent(reservation.Id, cmd.SkuId, reservation.Quantity), ct);
    }
}
