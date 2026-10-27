using Nexus.NotificationService.Domain.Entities;

namespace Nexus.NotificationService.Domain.Repositories;

/// <summary>
/// Repository interface cho Notification entity.
/// GetPendingRetriableAsync dùng bởi background retry job,
/// các method còn lại dùng bởi consumers và API controller.
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách notification của user với phân trang.
    /// OrderByDescending theo CreatedAt để notification mới nhất lên đầu.
    /// </summary>
    Task<List<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Lấy các notification Failed có thể retry (RetryCount &lt; maxRetries).
    /// Dùng cho background job chạy định kỳ để retry gửi lại các notification thất bại.
    /// </summary>
    Task<List<Notification>> GetPendingRetriableAsync(int maxRetries, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Port interface cho email provider.
/// Hiện implement bằng StubEmailSender, sau này đổi sang SendGrid/SES.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Port interface cho push notification provider.
/// Hiện implement bằng StubPushSender, sau này đổi sang Firebase FCM.
/// </summary>
public interface IPushSender
{
    Task SendAsync(Guid userId, string title, string body, CancellationToken ct = default);
}
