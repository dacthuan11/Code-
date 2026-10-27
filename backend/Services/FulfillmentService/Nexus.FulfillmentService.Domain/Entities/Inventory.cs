using Nexus.Shared.Domain;

namespace Nexus.FulfillmentService.Domain.Entities;

/// <summary>
/// Bản ghi tồn kho cho một SKU tại một warehouse cụ thể.
/// AvailableQuantity được tính động (computed) thay vì lưu DB
/// để tránh tình trạng out-of-sync khi có concurrent updates.
/// </summary>
public class InventoryRecord : BaseEntity
{
    public Guid SkuId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int TotalQuantity { get; private set; }

    // Số lượng đang bị "giữ chỗ" cho các đơn hàng chưa xác nhận
    public int ReservedQuantity { get; private set; }

    // Số lượng không thể bán: hàng hỏng, hết hạn, đang kiểm đếm...
    public int UnavailableQuantity { get; private set; }

    // Computed - không persist xuống DB, tính trực tiếp mỗi lần đọc
    // Available = Total - Reserved - Unavailable
    public int AvailableQuantity => TotalQuantity - ReservedQuantity - UnavailableQuantity;

    // EF Core cần private constructor để materialize entity từ DB
    private InventoryRecord() { }

    public static InventoryRecord Create(Guid skuId, Guid warehouseId, int total) => new()
    { SkuId = skuId, WarehouseId = warehouseId, TotalQuantity = total };

    /// <summary>
    /// Giữ chỗ (soft hold) số lượng hàng cho một đơn hàng.
    /// Throw InvalidOperationException nếu available không đủ
    /// - caller phải check trước hoặc handle exception ở tầng Application.
    /// </summary>
    public void Reserve(int qty)
    {
        if (AvailableQuantity < qty) throw new InvalidOperationException("Insufficient stock.");
        ReservedQuantity += qty;
        Touch(); // Touch() từ BaseEntity - cập nhật UpdatedAt timestamp
    }

    /// <summary>
    /// Giải phóng reserved quantity khi đơn bị cancel hoặc hết timeout.
    /// Math.Max(0,...) để phòng thủ: tránh ReservedQuantity âm do
    /// concurrent calls hoặc bug nghiệp vụ.
    /// </summary>
    public void Release(int qty) { ReservedQuantity = Math.Max(0, ReservedQuantity - qty); Touch(); }

    /// <summary>
    /// Xác nhận xuất kho thực sự - giảm cả Reserved lẫn Total.
    /// Gọi sau khi đơn hàng đã được duyệt và chuẩn bị giao.
    /// Khác với Release: Release chỉ bỏ hold, còn Commit thực sự giảm stock.
    /// </summary>
    public void Commit(int qty)
    {
        ReservedQuantity = Math.Max(0, ReservedQuantity - qty);
        TotalQuantity = Math.Max(0, TotalQuantity - qty); // Hàng đã ra kho thật
        Touch();
    }

    /// <summary>
    /// Điều chỉnh tồn kho thủ công - nhập thêm (delta > 0) hoặc ghi nhận hao hụt (delta &lt; 0).
    /// Dùng khi nhân viên kho nhập hàng về hoặc xử lý hàng bị hỏng.
    /// </summary>
    public void Adjust(int delta) { TotalQuantity = Math.Max(0, TotalQuantity + delta); Touch(); }
}

/// <summary>
/// Đại diện cho một lần giữ chỗ tồn kho gắn với một đơn hàng hoặc phiên đấu giá.
/// Reservation có TTL (time-to-live) - nếu không Commit trước ExpiresAt thì
/// một background job sẽ tự Release để tránh treo stock mãi mãi.
/// </summary>
public class InventoryReservation : BaseEntity
{
    public Guid SkuId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int Quantity { get; private set; }

    // Format: "Order:{orderId}" hoặc "Auction:{auctionId}" - dùng string thay Guid
    // để flexible hơn khi có nhiều loại reference
    public string Reference { get; private set; } = default!; // OrderId or AuctionId

    public DateTime ExpiresAt { get; private set; }

    // Computed - dùng UTC để nhất quán với các service khác trong cluster
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    // Sau khi Commit hoặc Release thì mark là true - không xóa record để audit
    public bool IsReleased { get; private set; }

    private InventoryReservation() { }

    /// <summary>
    /// Tạo reservation với timeout mặc định 30 phút.
    /// 30 phút là con số chọn tùy nghiệp vụ - đủ để user thanh toán mà không quá lâu.
    /// </summary>
    public static InventoryReservation Create(Guid skuId, Guid warehouseId, int qty, string reference, int timeoutMinutes = 30)
        => new() { SkuId = skuId, WarehouseId = warehouseId, Quantity = qty, Reference = reference, ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes) };

    // Dùng cho cả hai trường hợp: commit xong (hàng ra kho) và cancel (trả lại stock)
    public void MarkReleased() { IsReleased = true; Touch(); }
}

/// <summary>
/// Audit trail cho mọi biến động tồn kho - append-only, không bao giờ update hay delete.
/// Mỗi hành động (INTAKE, RESERVE, RELEASE, COMMIT, ADJUST) đều ghi một dòng vào đây.
/// Rất hữu ích khi cần truy vết lý do tồn kho sai, hoặc xử lý khiếu nại.
/// </summary>
public class InventoryLedger : BaseEntity
{
    public Guid SkuId { get; private set; }
    public Guid WarehouseId { get; private set; }

    // Loại biến động: INTAKE | RESERVE | RELEASE | COMMIT | ADJUST
    public string MovementType { get; private set; } = default!;

    // Quy ước: âm = hàng ra, dương = hàng vào (nhất quán với kế toán)
    public int Quantity { get; private set; }

    // Ghi chú tham chiếu: OrderId, AdjustmentNote, v.v.
    public string Reference { get; private set; } = default!;

    private InventoryLedger() { }

    public static InventoryLedger Create(Guid skuId, Guid warehouseId, string type, int qty, string reference)
        => new() { SkuId = skuId, WarehouseId = warehouseId, MovementType = type, Quantity = qty, Reference = reference };
}
