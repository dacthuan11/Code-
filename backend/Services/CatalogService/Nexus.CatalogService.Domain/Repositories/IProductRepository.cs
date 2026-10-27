using Nexus.CatalogService.Domain.Entities;

namespace Nexus.CatalogService.Domain.Repositories;

/// <summary>
/// Repository interface cho Product - nằm ở Domain layer để không phụ thuộc EF Core.
/// Implementation thực tế ở Infrastructure/Persistence/Repositories/.
/// Pattern này cho phép swap database mà Application/Domain không cần thay đổi.
/// </summary>
public interface IProductRepository
{
    /// <summary>Lấy product theo Id, trả null nếu không tìm thấy hoặc đã bị soft-delete.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Search sản phẩm với nhiều filter + phân trang.
    /// Trả về tuple (items, total) thay vì PagedResult riêng để giữ domain đơn giản.
    /// </summary>
    Task<(List<Product> Items, int Total)> SearchAsync(
        string? keyword, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice,
        string? sort, int page, int pageSize,
        CancellationToken ct = default);

    /// <summary>Lấy tất cả product của một seller - dùng cho trang quản lý của seller.</summary>
    Task<List<Product>> GetBySellerIdAsync(Guid sellerId, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);

    // SaveChanges tách riêng để handler control được transaction boundary
    // Một handler có thể gọi nhiều repo operations trước khi commit một lần
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface cho Category.
/// Tách riêng IProductRepository để Single Responsibility - mỗi interface chỉ lo một aggregate.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lấy tất cả category, sort theo Level rồi Name - dùng cho dropdown/tree UI.</summary>
    Task<List<Category>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Lấy các category con trực tiếp của một parent - dùng để render tree từng cấp.</summary>
    Task<List<Category>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);

    Task AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
