using Nexus.CommerceService.Domain.Entities;

namespace Nexus.CommerceService.Domain.Repositories;

// Interface repository định nghĩa ở tầng Domain - không phụ thuộc EF Core hay bất kỳ ORM nào.
// Implementation cụ thể nằm ở Infrastructure layer (dependency inversion).
// Mục đích: Application layer chỉ cần biết "cần làm gì" chứ không biết "làm như thế nào".

public interface ICartRepository
{
    // Lấy cart theo ID - trả null nếu không tìm thấy (không throw exception)
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Lấy cart đang active của user - chỉ lấy cart chưa expired và chưa deleted
    // Mỗi user chỉ có 1 active cart tại một thời điểm
    Task<Cart?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    // Add mới - chưa lưu DB ngay, cần gọi SaveChangesAsync
    Task AddAsync(Cart cart, CancellationToken ct = default);

    // Update - mark entity là modified, cần gọi SaveChangesAsync
    Task UpdateAsync(Cart cart, CancellationToken ct = default);

    Task AddCartItemAsync(CartItem item, CancellationToken ct = default);

    // Flush tất cả changes pending xuống DB trong một transaction
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Lấy danh sách orders của user theo pagination
    // Trả về tuple (danh sách items, tổng count) để FE hiển thị pagination
    Task<(List<Order> Items, int Total)> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
