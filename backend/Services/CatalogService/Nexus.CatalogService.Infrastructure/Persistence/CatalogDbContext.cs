using Microsoft.EntityFrameworkCore;
using Nexus.CatalogService.Application.Persistence;
using Nexus.CatalogService.Domain.Entities;

namespace Nexus.CatalogService.Infrastructure.Persistence;

/// <summary>
/// DbContext chính của CatalogService - chỉ chứa Product và Category.
/// Mỗi microservice có DbContext riêng, không share database với service khác.
/// </summary>
public class CatalogDbContext : DbContext, ICatalogDatabase
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product>       Products       => Set<Product>();
    public DbSet<Category>      Categories     => Set<Category>();
    public DbSet<ProductSku>    ProductSkus    => Set<ProductSku>();
    public DbSet<SkuAttribute>  SkuAttributes  => Set<SkuAttribute>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);

            // Dùng decimal(18,2) thay vì float để tránh rounding error khi tính tiền
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");

            // Lưu enum dạng string thay vì int để dễ đọc trong DB
            // Trade-off: query chậm hơn một chút so với int, nhưng debug dễ hơn nhiều
            e.Property(p => p.Status).HasConversion<string>();

            // ImageUrls là List<string> - EF không hỗ trợ native, nên serialize thủ công
            // Dùng pipe '|' làm separator thay vì ',' để tránh URL chứa dấu phẩy bị lỗi
            e.Property(p => p.ImageUrls)
                .HasConversion(
                    v => string.Join('|', v),                                              // khi lưu: List → "url1|url2|url3"
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()      // khi đọc: string → List
                );

            // Global query filter: mọi query trên Products đều tự động thêm WHERE IsDeleted = false
            // Không cần viết điều kiện này ở mọi chỗ, tránh bug quên filter
            // Muốn query kể cả deleted thì dùng .IgnoreQueryFilters()
            e.HasQueryFilter(p => !p.IsDeleted);

            // Mỗi product thuộc 1 category, category có nhiều product (nhưng không có nav prop ngược)
            e.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
        });

        mb.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.HasQueryFilter(c => !c.IsDeleted);
            e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId);
        });

        mb.Entity<ProductSku>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.SkuCode).IsRequired().HasMaxLength(50);
            e.Property(s => s.Price).HasColumnType("decimal(18,2)");
            e.HasIndex(s => new { s.ProductId, s.SkuCode }).IsUnique();
            e.HasOne<Product>().WithMany(p => p.Skus)
                .HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<SkuAttribute>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(50);
            e.Property(a => a.Value).HasMaxLength(100);
            e.HasOne<ProductSku>().WithMany(s => s.Attributes)
                .HasForeignKey(a => a.SkuId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ProductReview>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Comment).HasMaxLength(2000);
            e.Property(r => r.ReviewerName).HasMaxLength(100);
            e.HasIndex(r => new { r.ReviewerId, r.OrderItemId }).IsUnique(); // mỗi orderItem review 1 lần
            e.HasIndex(r => r.ProductId);
            e.Property(r => r.ImageUrls).HasConversion(
                v => string.Join('|', v),
                v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());
            e.HasOne<Product>().WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Product>(e2 =>
        {
            e2.Property(p => p.AverageRating).HasColumnType("decimal(3,2)");
        });
    }
}
