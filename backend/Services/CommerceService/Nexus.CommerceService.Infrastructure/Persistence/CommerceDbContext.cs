using Microsoft.EntityFrameworkCore;
using Nexus.CommerceService.Application.Persistence;
using Nexus.CommerceService.Domain.Entities;

namespace Nexus.CommerceService.Infrastructure.Persistence;

// DbContext cho CommerceService - mỗi service có DbContext riêng (database per service pattern).
// Không share DbContext với service khác để đảm bảo bounded context isolation.
public class CommerceDbContext : DbContext, ICommerceDatabase
{
    public CommerceDbContext(DbContextOptions<CommerceDbContext> options) : base(options) { }

    // DbSet - EF Core dùng các property này để biết table nào cần tạo/query
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherUsage> VoucherUsages => Set<VoucherUsage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Cart>(e =>
        {
            e.HasKey(c => c.Id);
            // Index trên UserId để GetActiveByUserIdAsync chạy nhanh
            e.HasIndex(c => c.UserId);

            // Cascade delete: xoá Cart thì xoá luôn CartItems
            // Hợp lý vì CartItem không có ý nghĩa nếu không có Cart
            e.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);

            // Lưu Status dạng string ("PendingPayment", "Paid", ...) thay vì int
            // Dễ debug hơn trong DB, không sợ bị lệch số khi thêm/xoá enum value
            e.Property(o => o.Status).HasConversion<string>();

            // Chỉ định precision để tránh warning + đảm bảo không bị mất phần thập phân
            e.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            // Tương tự Cart - OrderItem chỉ tồn tại khi có Order
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(o => o.StatusHistory).WithOne().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Status).HasConversion<string>();
            e.HasIndex(h => h.OrderId);
        });

        mb.Entity<Voucher>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => v.Code).IsUnique();
            e.HasIndex(v => v.SellerId);
            e.Property(v => v.Code).IsRequired().HasMaxLength(50);
            e.Property(v => v.Name).IsRequired().HasMaxLength(200);
            e.Property(v => v.DiscountType).HasConversion<string>();
            e.Property(v => v.Scope).HasConversion<string>();
            e.Property(v => v.DiscountValue).HasColumnType("decimal(18,2)");
            e.Property(v => v.MaxDiscount).HasColumnType("decimal(18,2)");
            e.Property(v => v.MinOrderValue).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(v => !v.IsDeleted);
        });

        mb.Entity<VoucherUsage>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.VoucherId, u.UserId });
            e.HasIndex(u => u.OrderId);
            e.Property(u => u.DiscountAmount).HasColumnType("decimal(18,2)");
        });
    }
}
