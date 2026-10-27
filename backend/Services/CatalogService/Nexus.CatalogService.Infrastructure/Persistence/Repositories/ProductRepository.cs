using Microsoft.EntityFrameworkCore;
using Nexus.CatalogService.Domain.Entities;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation của IProductRepository dùng EF Core + PostgreSQL.
/// Global query filter trong DbContext đã handle soft delete, nên không cần thêm !IsDeleted ở đây.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _db;
    public ProductRepository(CatalogDbContext db) => _db = db;

    // FirstOrDefaultAsync tự động áp dụng HasQueryFilter (!IsDeleted) từ DbContext
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>
    /// Search với nhiều filter tùy chọn - chỉ trả Active products.
    /// Build query dần theo kiểu chain LINQ, EF sẽ translate sang 1 SQL query duy nhất.
    /// </summary>
    public async Task<(List<Product> Items, int Total)> SearchAsync(
        string? keyword, Guid? categoryId, decimal? minPrice, decimal? maxPrice,
        string? sort, int page, int pageSize, CancellationToken ct = default)
    {
        // Chỉ search sản phẩm đang Active - Draft/Inactive không hiện ra ngoài
        var q = _db.Products.Where(p => p.Status == ProductStatus.Active);

        // Mỗi filter chỉ apply khi có giá trị - tránh SQL "WHERE 1=1" không cần thiết
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
        if (minPrice.HasValue)   q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)   q = q.Where(p => p.Price <= maxPrice.Value);

        // Switch expression cho sort - default là mới nhất trước
        // "price_asc" và "price_desc" dùng cho trang kết quả search
        q = sort switch
        {
            "price_asc"  => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            _            => q.OrderByDescending(p => p.CreatedAt)  // default: mới nhất
        };

        // Đếm total TRƯỚC khi Skip/Take để lấy đúng tổng số record thỏa điều kiện
        var total = await q.CountAsync(ct);

        // Phân trang: skip (page-1)*pageSize record đầu, lấy pageSize record tiếp theo
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    // Lấy tất cả product của seller - không filter theo status, seller thấy cả Draft
    public Task<List<Product>> GetBySellerIdAsync(Guid sellerId, CancellationToken ct = default) =>
        _db.Products.Where(p => p.SellerId == sellerId).ToListAsync(ct);

    // AddAsync: cần await vì AddAsync trả ValueTask trong EF Core
    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);

    // Update không async vì EF chỉ mark entity là Modified trong memory, không gọi DB
    // DB thực sự được update khi gọi SaveChangesAsync
    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>
/// Implementation của ICategoryRepository.
/// Đặt chung file với ProductRepository vì cả hai đều liên quan đến Catalog context.
/// Nếu sau này logic phức tạp hơn thì tách file riêng.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly CatalogDbContext _db;
    public CategoryRepository(CatalogDbContext db) => _db = db;

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    // Sort theo Level rồi Name để kết quả nhất quán khi render tree/dropdown
    public Task<List<Category>> GetAllAsync(CancellationToken ct = default) =>
        _db.Categories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToListAsync(ct);

    // Lấy children trực tiếp (không đệ quy) - UI tự load từng cấp khi cần
    public Task<List<Category>> GetChildrenAsync(Guid parentId, CancellationToken ct = default) =>
        _db.Categories.Where(c => c.ParentId == parentId).ToListAsync(ct);

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await _db.Categories.AddAsync(category, ct);

    public Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Update(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
