using MediatR;
using Nexus.FulfillmentService.Domain.Entities;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.UseCases.ReserveInventory;

/// <summary>
/// Xử lý Reserve Inventory - bước đầu tiên trong luồng checkout.
/// Thứ tự: check stock → cập nhật InventoryRecord → tạo Reservation → ghi Ledger → publish event.
/// Tất cả trong một SaveChanges để đảm bảo atomic.
/// </summary>
public class ReserveInventoryHandler : IRequestHandler<ReserveInventoryCommand, ReserveInventoryResult>
{
    private readonly IInventoryRepository _inventory;
    private readonly IEventBus _eventBus;

    public ReserveInventoryHandler(IInventoryRepository inventory, IEventBus eventBus)
    {
        _inventory = inventory;
        _eventBus = eventBus;
    }

    public async Task<ReserveInventoryResult> Handle(ReserveInventoryCommand cmd, CancellationToken ct)
    {
        // Tìm record tồn kho - không cần biết warehouse vì GetBySku trả về kho đầu tiên có SKU này
        var record = await _inventory.GetBySkuAsync(cmd.SkuId, ct)
            ?? throw new KeyNotFoundException($"SKU {cmd.SkuId} not found");

        // Kiểm tra trước khi gọi domain method để trả message rõ ràng hơn
        if (record.AvailableQuantity < cmd.Quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {record.AvailableQuantity}, Requested: {cmd.Quantity}");

        // InventoryRecord.Reserve chỉ nhận 1 tham số và không return reservation
        // Nó chỉ tăng ReservedQuantity trên record - không tạo reservation object
        record.Reserve(cmd.Quantity);

        // CustomReference override mặc định - auction dùng "Auction:{id}", order dùng "Order:{id}"
        var reference = cmd.CustomReference ?? $"Order:{cmd.OrderId}";

        var reservation = InventoryReservation.Create(cmd.SkuId, record.WarehouseId, cmd.Quantity, reference, cmd.TimeoutMinutes);
        await _inventory.AddReservationAsync(reservation, ct);

        await _inventory.AppendLedgerAsync(cmd.SkuId, "RESERVE", -cmd.Quantity, $"Reserved: {reference}", ct);

        // Một lần SaveChanges duy nhất - nếu có lỗi thì rollback toàn bộ
        await _inventory.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new InventoryReservedEvent(reservation.Id, cmd.SkuId, cmd.Quantity, reference), ct);

        return new ReserveInventoryResult(reservation.Id, reservation.ExpiresAt);
    }
}
