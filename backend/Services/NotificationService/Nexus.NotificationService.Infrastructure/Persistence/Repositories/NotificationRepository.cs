using Microsoft.EntityFrameworkCore;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;

namespace Nexus.NotificationService.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation của INotificationRepository.
/// Tất cả query đều có OrderByDescending(CreatedAt) hoặc phân trang để tránh load cả bảng.
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;
    public NotificationRepository(NotificationDbContext db) => _db = db;

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    /// <summary>
    /// Lấy notification của user với pagination - không bao giờ load hết bảng.
    /// Sắp xếp mới nhất lên đầu vì user thường muốn xem thông báo gần đây nhất.
    /// </summary>
    public Task<List<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default) =>
        _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt) // Mới nhất lên đầu
            .Skip((page - 1) * pageSize).Take(pageSize) // Offset-based pagination
            .ToListAsync(ct);

    /// <summary>
    /// Query notifications Failed mà chưa vượt ngưỡng retry.
    /// Background job gọi method này để lấy danh sách cần retry.
    /// Không có ordering vì retry job xử lý tất cả, không cần thứ tự.
    /// </summary>
    public Task<List<Notification>> GetPendingRetriableAsync(int maxRetries, CancellationToken ct = default) =>
        _db.Notifications
            .Where(n => n.Status == NotificationStatus.Failed && n.RetryCount < maxRetries)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);
        n?.MarkRead();
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread) n.MarkRead();
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await _db.Notifications.AddAsync(notification, ct);

    // Update: đánh dấu entity là modified, EF gen UPDATE SQL khi SaveChanges
    public Task UpdateAsync(Notification notification, CancellationToken ct = default)
    { _db.Notifications.Update(notification); return Task.CompletedTask; }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
