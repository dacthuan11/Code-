using Microsoft.EntityFrameworkCore;
using Nexus.FulfillmentService.Application.Persistence;
using Nexus.FulfillmentService.Domain.Entities;

namespace Nexus.FulfillmentService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext cho FulfillmentService.
/// Dùng Postgres (Npgsql) - xem connection string trong appsettings.json.
/// Fluent API thay vì Data Annotation để giữ Domain entities sạch khỏi EF attributes.
/// </summary>
public class FulfillmentDbContext : DbContext, IFulfillmentDatabase
{
    public FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options) : base(options) { }

    // DbSet properties dùng expression body để ngắn gọn hơn
    public DbSet<InventoryRecord> Inventories => Set<InventoryRecord>();
    public DbSet<InventoryReservation> Reservations => Set<InventoryReservation>();
    public DbSet<InventoryLedger> Ledgers => Set<InventoryLedger>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<InventoryRecord>(e =>
        {
            e.HasKey(i => i.Id);
            // Unique index để đảm bảo mỗi SKU chỉ có một record tại mỗi warehouse
            // Tránh nhập trùng dẫn đến stock bị tính double
            e.HasIndex(i => new { i.SkuId, i.WarehouseId }).IsUnique();
        });

        mb.Entity<InventoryReservation>(e => e.HasKey(r => r.Id));

        mb.Entity<InventoryLedger>(e =>
        {
            e.HasKey(l => l.Id);
            // Ledger is append-only — no updates allowed via EF
            // Đặt tên table rõ ràng để dễ query trong Postgres
            e.ToTable("inventory_ledger");
        });

        mb.Entity<Shipment>(e =>
        {
            e.HasKey(s => s.Id);
            // Lưu enum dạng string thay vì int để dễ đọc trực tiếp trong DB
            e.Property(s => s.Status).HasConversion<string>();
            // Precision 18,2 cho tiền VNĐ - không dùng float/double vì mất precision
            e.Property(s => s.ShippingFee).HasColumnType("decimal(18,2)");
        });

        // Map InventoryRecord sang table tên "inventory" (snake_case convention)
        mb.Entity<InventoryRecord>().ToTable("inventory");

        mb.Entity<ReturnRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.OrderId);
            e.HasIndex(r => r.SellerId);
            e.HasIndex(r => r.BuyerId);
            e.Property(r => r.Reason).HasConversion<string>();
            e.Property(r => r.Status).HasConversion<string>();
            e.Property(r => r.Description).HasMaxLength(2000);
            e.Property(r => r.SellerNote).HasMaxLength(1000);
            e.Property(r => r.RefundAmount).HasColumnType("decimal(18,2)");
            e.Property(r => r.EvidenceImageUrls).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            e.HasQueryFilter(r => !r.IsDeleted);
        });

        mb.Entity<ShipmentTrackingEvent>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.ShipmentId);
            e.Property(t => t.Status).IsRequired().HasMaxLength(100);
            e.Property(t => t.Description).IsRequired().HasMaxLength(500);
            e.Property(t => t.Location).HasMaxLength(300);
        });
    }
}
