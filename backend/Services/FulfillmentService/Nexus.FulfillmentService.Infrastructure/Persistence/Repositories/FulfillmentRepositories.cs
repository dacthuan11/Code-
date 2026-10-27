using Microsoft.EntityFrameworkCore;
using Nexus.FulfillmentService.Domain.Entities;
using Nexus.FulfillmentService.Domain.Repositories;

namespace Nexus.FulfillmentService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation của IInventoryRepository dùng EF Core + Postgres.
/// Tất cả query đều async để không block thread pool.
/// SaveChanges được gọi riêng ở tầng handler để control transaction boundary.
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    private readonly FulfillmentDbContext _db;
    public InventoryRepository(FulfillmentDbContext db) => _db = db;

    // Tìm bất kỳ record nào có SKU này - không quan tâm warehouse
    public Task<InventoryRecord?> GetBySkuAsync(Guid skuId, CancellationToken ct = default) =>
        _db.Inventories.FirstOrDefaultAsync(i => i.SkuId == skuId, ct);

    // Tìm chính xác SKU + warehouse - dùng khi cần xác định kho cụ thể
    public Task<InventoryRecord?> GetBySkuAndWarehouseAsync(Guid skuId, Guid warehouseId, CancellationToken ct = default) =>
        _db.Inventories.FirstOrDefaultAsync(i => i.SkuId == skuId && i.WarehouseId == warehouseId, ct);

    // InventoryReservation dùng Reference (string) thay vì OrderId Guid trực tiếp
    // Phải filter thêm !IsReleased để chỉ lấy reservation đang active
    public Task<InventoryReservation?> GetReservationByOrderAsync(Guid orderId, Guid skuId, CancellationToken ct = default) =>
        _db.Reservations.FirstOrDefaultAsync(r => r.Reference == $"Order:{orderId}" && r.SkuId == skuId && !r.IsReleased, ct);

    public Task<List<InventoryReservation>> GetActiveReservationsByReferenceAsync(string reference, CancellationToken ct = default) =>
        _db.Reservations.Where(r => r.Reference == reference && !r.IsReleased).ToListAsync(ct);

    public Task<List<InventoryReservation>> GetExpiredActiveReservationsAsync(CancellationToken ct = default) =>
        _db.Reservations.Where(r => !r.IsReleased && r.ExpiresAt <= DateTime.UtcNow).ToListAsync(ct);

    public async Task AddReservationAsync(InventoryReservation reservation, CancellationToken ct = default) =>
        await _db.Reservations.AddAsync(reservation, ct);

    // Update: chỉ đánh dấu entity là modified, EF sẽ generate UPDATE khi SaveChanges
    public Task UpdateReservationAsync(InventoryReservation reservation, CancellationToken ct = default)
    { _db.Reservations.Update(reservation); return Task.CompletedTask; }

    /// <summary>
    /// Ghi một entry vào ledger - tạo InventoryLedger entity và add vào context.
    /// WarehouseId dùng Guid.Empty tạm thời - TODO: truyền đúng warehouseId xuống.
    /// </summary>
    public async Task AppendLedgerAsync(Guid skuId, string movementType, int delta, string note, CancellationToken ct = default)
    {
        // InventoryLedger.Create nhận (skuId, warehouseId, type, qty, reference) - dùng factory method
        var entry = InventoryLedger.Create(skuId, Guid.Empty, movementType, delta, note);
        await _db.Ledgers.AddAsync(entry, ct);
    }

    public async Task<Dictionary<Guid, int>> GetAvailableStockBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default)
    {
        var ids = skuIds.ToList();
        var records = await _db.Inventories
            .Where(i => ids.Contains(i.SkuId))
            .ToListAsync(ct);
        return records.ToDictionary(
            r => r.SkuId,
            r => Math.Max(0, r.TotalQuantity - r.ReservedQuantity - r.UnavailableQuantity));
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>
/// Implementation của IShipmentRepository.
/// Hỗ trợ lookup theo cả Id và OrderId vì client thường chỉ có OrderId.
/// </summary>
public class ShipmentRepository : IShipmentRepository
{
    private readonly FulfillmentDbContext _db;
    public ShipmentRepository(FulfillmentDbContext db) => _db = db;

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);

    // Thường dùng hơn GetByIdAsync vì caller biết OrderId trước
    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId, ct);

    public async Task AddAsync(Shipment shipment, CancellationToken ct = default) => await _db.Shipments.AddAsync(shipment, ct);

    public Task UpdateAsync(Shipment shipment, CancellationToken ct = default) { _db.Shipments.Update(shipment); return Task.CompletedTask; }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
