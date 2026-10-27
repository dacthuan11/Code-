using Microsoft.EntityFrameworkCore;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Domain.Repositories;

namespace Nexus.CommerceService.Infrastructure.Persistence.Repositories;

// CartRepository: implementation cụ thể của ICartRepository dùng EF Core.
// Tầng Infrastructure - Application layer không biết class này tồn tại,
// chỉ biết ICartRepository interface.
public class CartRepository : ICartRepository
{
    private readonly CommerceDbContext _db;
    public CartRepository(CommerceDbContext db) => _db = db;

    // Include(c => c.Items) để load eager - tránh lazy loading N+1 query
    // Mỗi lần get cart là cần Items ngay luôn nên eager load là đúng
    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);

    // GetActiveByUserIdAsync: lấy cart chưa bị xoá VÀ chưa hết hạn
    // 2 điều kiện: IsDeleted = false (soft delete) + ExpiresAt > now (time-based expiry)
    public Task<Cart?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow, ct);

    // AddAsync chỉ thêm vào EF tracking context, chưa lưu DB - cần gọi SaveChangesAsync sau
    public async Task AddAsync(Cart cart, CancellationToken ct = default) => await _db.Carts.AddAsync(cart, ct);

    // Update đánh dấu entity là Modified trong EF tracking, chưa flush xuống DB
    public Task UpdateAsync(Cart cart, CancellationToken ct = default) { _db.Carts.Update(cart); return Task.CompletedTask; }

    public async Task AddCartItemAsync(CartItem item, CancellationToken ct = default)
        => await _db.CartItems.AddAsync(item, ct);

    // Flush tất cả pending changes trong một DB transaction
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

// OrderRepository: tương tự CartRepository nhưng cho Order
public class OrderRepository : IOrderRepository
{
    private readonly CommerceDbContext _db;
    public OrderRepository(CommerceDbContext db) => _db = db;

    // Include items + StatusHistory để EF Core track đúng → RecordHistory không bị lỗi Modified-on-insert
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory).FirstOrDefaultAsync(o => o.Id == id, ct);

    // Pagination query - cần đếm total trước rồi mới lấy page
    // OrderByDescending(CreatedAt) để order mới nhất hiển thị trước
    public async Task<(List<Order> Items, int Total)> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt);

        // Đếm total trước (1 query) rồi mới lấy page (1 query) = 2 queries tổng
        // Không dùng ToListAsync rồi Count() sau vì sẽ load hết dữ liệu vào memory
        var total = await q.CountAsync(ct);
        var items = await q.Include(o => o.Items).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default) => await _db.Orders.AddAsync(order, ct);
    // Không gọi Update() khi entity đã được tracked bởi GetByIdAsync —
    // Update() sẽ đánh dấu tất cả navigation entities (kể cả entity mới) là Modified,
    // khiến EF Core cố UPDATE row chưa tồn tại → DbUpdateConcurrencyException.
    // Khi tracked, EF Core tự phát hiện thay đổi qua change tracker; chỉ Update() khi detached.
    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        if (_db.Entry(order).State == EntityState.Detached)
            _db.Orders.Update(order);
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
