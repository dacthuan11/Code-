using Microsoft.EntityFrameworkCore;
using Nexus.AuctionService.Application.Persistence;
using Nexus.AuctionService.Domain.Entities;

namespace Nexus.AuctionService.Infrastructure.Persistence;

// EF Core DbContext cho AuctionService - mỗi microservice có DbContext riêng
// Không share database với service khác (Database-per-Service pattern của microservices)
public class AuctionDbContext : DbContext, IAuctionDatabase
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    public DbSet<Auction>      Auctions      => Set<Auction>();
    public DbSet<Bid>          Bids          => Set<Bid>();
    public DbSet<FlashSale>    FlashSales    => Set<FlashSale>();
    public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Auction>(e =>
        {
            e.HasKey(a => a.Id);

            // Lưu Status dưới dạng string thay vì int - dễ debug trực tiếp trong DB
            // Trade-off: tốn thêm storage nhưng readable hơn nhiều khi query tay
            e.Property(a => a.Status).HasConversion<string>();

            // Decimal cần explicit precision để tránh EF warning và đảm bảo khớp với DB schema
            e.Property(a => a.StartingPrice).HasColumnType("decimal(18,2)");
            e.Property(a => a.CurrentPrice).HasColumnType("decimal(18,2)");
            e.Property(a => a.BidIncrement).HasColumnType("decimal(18,2)");

            // Cascade delete: xóa Auction thì xóa hết Bids - hợp lý vì Bid không tồn tại độc lập
            e.HasMany(a => a.Bids).WithOne().HasForeignKey(b => b.AuctionId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Bid>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Amount).HasColumnType("decimal(18,2)");
        });

        mb.Entity<FlashSale>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.SellerId);
            e.Property(f => f.Name).IsRequired().HasMaxLength(200);
            e.Property(f => f.Status).HasConversion<string>();
            e.HasMany(f => f.Items).WithOne().HasForeignKey(i => i.FlashSaleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(f => !f.IsDeleted);
        });

        mb.Entity<FlashSaleItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.FlashSaleId);
            e.HasIndex(i => i.SkuId);
            e.Property(i => i.OriginalPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.FlashSalePrice).HasColumnType("decimal(18,2)");
        });
    }
}
