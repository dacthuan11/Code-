using MediatR;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.UseCases.CommitInventory;

/// <summary>
/// Xử lý Commit Inventory - bước cuối của luồng checkout khi payment thành công.
/// Sau Commit: reservation bị mark released, tổng stock giảm thật.
/// Không thể undo sau bước này (phải dùng luồng Return/Refund riêng).
/// </summary>
public class CommitInventoryHandler : IRequestHandler<CommitInventoryCommand>
{
    private readonly IInventoryRepository _inventory;
    private readonly IEventBus _eventBus;

    public CommitInventoryHandler(IInventoryRepository inventory, IEventBus eventBus)
    {
        _inventory = inventory;
        _eventBus = eventBus;
    }

    public async Task Handle(CommitInventoryCommand cmd, CancellationToken ct)
    {
        // Tìm reservation đang active - nếu không có thì throw vì có thể đã expired hoặc released rồi
        var reservation = await _inventory.GetReservationByOrderAsync(cmd.OrderId, cmd.SkuId, ct)
            ?? throw new KeyNotFoundException($"No reservation for order {cmd.OrderId} / SKU {cmd.SkuId}");

        // Cần record để gọi domain method Commit - không thể Commit qua reservation trực tiếp
        var record = await _inventory.GetBySkuAsync(cmd.SkuId, ct)
            ?? throw new KeyNotFoundException($"SKU {cmd.SkuId} not found");

        // Domain model dùng Commit(int qty) + MarkReleased() riêng trên reservation
        // Hai bước tách biệt vì InventoryRecord và InventoryReservation là hai aggregate riêng
        record.Commit(reservation.Quantity);
        reservation.MarkReleased(); // Đánh dấu reservation đã dùng xong

        // Update reservation trước, save chung một lần
        await _inventory.UpdateReservationAsync(reservation, ct);

        // Ghi ledger - delta âm vì đây là xuất kho thật (committed out)
        await _inventory.AppendLedgerAsync(cmd.SkuId, "COMMIT", -reservation.Quantity, $"Committed for order {cmd.OrderId}", ct);
        await _inventory.SaveChangesAsync(ct);

        // Publish event để các service khác biết (ví dụ FulfillmentService tạo shipment)
        await _eventBus.PublishAsync(new InventoryCommittedEvent(cmd.OrderId, cmd.SkuId, reservation.Quantity), ct);
    }
}
