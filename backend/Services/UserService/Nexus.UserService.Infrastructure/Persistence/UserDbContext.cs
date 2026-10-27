using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nexus.UserService.Application.Persistence;
using Nexus.UserService.Domain.Entities;

namespace Nexus.UserService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext cho UserService.
/// Tách riêng DbContext cho từng service (không share) để đúng với microservice pattern -
/// mỗi service có database riêng, schema riêng.
/// </summary>
public class UserDbContext : DbContext, IUserDatabase
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User>              Users          => Set<User>();
    public DbSet<Role>              Roles          => Set<Role>();
    public DbSet<RefreshToken>      RefreshTokens  => Set<RefreshToken>();
    public DbSet<Review>            Reviews        => Set<Review>();
    public DbSet<SellerProfile>     SellerProfiles => Set<SellerProfile>();
    public DbSet<UserAddress>       Addresses      => Set<UserAddress>();
    public DbSet<Wallet>            Wallets        => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTxs      => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Cấu hình entity User ──────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);

            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique(); // unique index để check duplicate nhanh

            e.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.IdentifyNumber).IsRequired().HasMaxLength(20);
            e.Property(u => u.PasswordHash).IsRequired();

            // decimal(5,2) cho ReputationScore: max 999.99 - thực tế chỉ 0-100
            e.Property(u => u.ReputationScore).HasColumnType("decimal(5,2)");

            // Global query filter: tất cả query trên Users sẽ tự động kèm WHERE IsDeleted = 0
            // Muốn lấy cả user đã xóa thì phải dùng .IgnoreQueryFilters()
            e.HasQueryFilter(u => !u.IsDeleted);

            // Relationship: User có 1 Role, Role có nhiều User
            e.HasOne(u => u.Role).WithMany().HasForeignKey(u => u.RoleId);
        });

        // ── Cấu hình entity Role ──────────────────────────────────────────────
        mb.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(100);
            e.Property(r => r.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Code).IsUnique(); // BUYER/SELLER/ADMIN phải unique

            // Privileges là List<string> nhưng SQLite/Postgres không có array type tốt,
            // nên lưu dạng comma-separated: "AUTH.LOGIN,CART.CREATE,ORDER.CREATE"
            // ValueComparer cần thiết để HasData() nhận List<string> đúng kiểu
            var listComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());
            e.Property(r => r.Privileges)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    listComparer);
        });

        mb.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => r.UserId);
            e.Property(r => r.Token).IsRequired().HasMaxLength(512);
        });

        mb.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.TargetUserId);
            e.HasIndex(r => r.ReviewerId);
            e.Property(r => r.Comment).IsRequired().HasMaxLength(1000);
        });

        mb.Entity<SellerProfile>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasIndex(s => s.ShopSlug).IsUnique();
            e.Property(s => s.ShopName).IsRequired().HasMaxLength(100);
            e.Property(s => s.ShopSlug).IsRequired().HasMaxLength(150);
            e.Property(s => s.Description).HasMaxLength(2000);
            e.Property(s => s.Rating).HasColumnType("decimal(3,1)");
        });

        mb.Entity<UserAddress>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.UserId);
            e.Property(a => a.RecipientName).IsRequired().HasMaxLength(100);
            e.Property(a => a.Phone).IsRequired().HasMaxLength(20);
            e.Property(a => a.AddressLine).IsRequired().HasMaxLength(300);
            e.Property(a => a.Label).HasMaxLength(50);
        });

        mb.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.UserId).IsUnique();
            e.Property(w => w.Balance).HasColumnType("decimal(18,2)");
            e.HasMany(w => w.Transactions).WithOne()
                .HasForeignKey(t => t.WalletId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<WalletTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.WalletId);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Type).HasConversion<string>();
            e.Property(t => t.Description).HasMaxLength(500);
            e.Property(t => t.Reference).HasMaxLength(200);
        });

        // ── Seed data cho 3 role mặc định ────────────────────────────────────
        // Dùng Guid cố định để migration idempotent (chạy nhiều lần không bị duplicate)
        var buyerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sellerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        mb.Entity<Role>().HasData(
            new Role(buyerRoleId, "Buyer", "BUYER", "Standard buyer account",
                ["AUTH.LOGIN","AUTH.LOGOUT","CART.CREATE","CART.UPDATE","CHECKOUT.START","ORDER.CREATE","AUCTION.BID"], seedDate),
            new Role(sellerRoleId, "Seller", "SELLER", "Seller account",
                ["AUTH.LOGIN","AUTH.LOGOUT","PRODUCT.CREATE","PRODUCT.UPDATE","PRODUCT.DELETE","AUCTION.CREATE","ORDER.VIEW"], seedDate),
            new Role(adminRoleId, "Admin", "ADMIN", "System administrator", ["ALL"], seedDate)
        );
    }
}
