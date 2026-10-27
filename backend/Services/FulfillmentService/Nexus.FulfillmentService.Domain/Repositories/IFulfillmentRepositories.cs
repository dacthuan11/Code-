using Nexus.FulfillmentService.Domain.Entities;

namespace Nexus.FulfillmentService.Domain.Repositories;

/// <summary>
/// Repository interface cho tồn kho.
/// Tách riêng GetBySku và GetBySkuAndWarehouse để linh hoạt:
/// - GetBySku: dùng khi không quan tâm warehouse (tìm bất kỳ kho nào có hàng)
/// - GetBySkuAndWarehouse: dùng khi cần xác định kho cụ thể
/// </summary>
public interface IInventoryRepository
{
    Task<InventoryRecord?> GetBySkuAsync(Guid skuId, CancellationToken ct = default);
    Task<InventoryRecord?> GetBySkuAndWarehouseAsync(Guid skuId, Guid warehouseId, CancellationToken ct = default);

    // Tìm reservation đang active (chưa released) của một đơn hàng + SKU cụ thể
    Task<InventoryReservation?> GetReservationByOrderAsync(Guid orderId, Guid skuId, CancellationToken ct = default);

    // Tìm tất cả reservation active theo Reference prefix (vd: "Order:{id}" hay "Auction:{id}")
    Task<List<InventoryReservation>> GetActiveReservationsByReferenceAsync(string reference, CancellationToken ct = default);

    // Tìm tất cả reservation đã hết hạn nhưng chưa released - dùng bởi background job
    Task<List<InventoryReservation>> GetExpiredActiveReservationsAsync(CancellationToken ct = default);

    Task AddReservationAsync(InventoryReservation reservation, CancellationToken ct = default);
    Task UpdateReservationAsync(InventoryReservation reservation, CancellationToken ct = default);

    /// <summary>
    /// Ghi một dòng vào inventory ledger - append-only, không bao giờ update.
    /// Tách thành method riêng thay vì để handler tự tạo InventoryLedger object
    /// để đảm bảo format reference nhất quán.
    /// </summary>
    Task AppendLedgerAsync(Guid skuId, string movementType, int delta, string note, CancellationToken ct = default);

    Task<Dictionary<Guid, int>> GetAvailableStockBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default);

    // Gộp SaveChanges vào interface để handler control transaction boundary
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface cho shipment.
/// Hỗ trợ lookup theo cả ShipmentId và OrderId vì các service khác
/// thường chỉ biết OrderId, không biết ShipmentId.
/// </summary>
public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task AddAsync(Shipment shipment, CancellationToken ct = default);
    Task UpdateAsync(Shipment shipment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
