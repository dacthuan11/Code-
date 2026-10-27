using Nexus.Shared.Domain;

namespace Nexus.NotificationService.Domain.Entities;

// Hiện tại có 2 channel: Email (cho thông báo tài khoản) và Push (cho real-time alerts)
// Sau này có thể thêm SMS, Zalo OA...
public enum NotificationChannel { Email, Push }

// Pending = vừa tạo chưa gửi; Sent = gửi thành công; Failed = gửi thất bại (có thể retry)
public enum NotificationStatus { Pending, Sent, Failed }

/// <summary>
/// Entity đại diện cho một thông báo gửi đến user.
/// Mỗi notification chỉ gửi qua 1 channel - nếu cần gửi cả email lẫn push
/// thì tạo 2 Notification records riêng biệt.
/// </summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }

    // Type string như "USER_REGISTERED", "ORDER_CREATED" - dùng string thay enum
    // để flexible: thêm type mới không cần migration DB
    public string Type { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;

    public NotificationChannel Channel { get; private set; }

    // Khởi đầu luôn là Pending, không được set thẳng trạng thái khác khi tạo
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;

    // Đếm số lần thử gửi thất bại - dùng để giới hạn retry (tránh spam loop)
    public int RetryCount { get; private set; }

    // Lưu error message gần nhất để debug, không null chỉ khi Status == Failed
    public string? ErrorMessage { get; private set; }

    private Notification() { }

    public static Notification Create(Guid userId, string type, string title, string body, NotificationChannel channel)
        => new() { UserId = userId, Type = type, Title = title, Body = body, Channel = channel };

    // Gọi sau khi gửi thành công qua email/push provider
    public void MarkSent() { Status = NotificationStatus.Sent; Touch(); }

    /// <summary>
    /// Đánh dấu gửi thất bại và tăng RetryCount.
    /// RetryCount tăng mỗi lần fail để background job biết đã thử bao nhiêu lần.
    /// </summary>
    public void MarkFailed(string error) { Status = NotificationStatus.Failed; ErrorMessage = error; RetryCount++; Touch(); }

    /// <summary>
    /// Kiểm tra notification này còn eligible để retry không.
    /// Dùng bởi background job quét các notification Failed chưa vượt ngưỡng.
    /// </summary>
    public bool CanRetry(int maxRetries) => RetryCount < maxRetries && Status == NotificationStatus.Failed;

    public bool IsRead { get; private set; } = false;
    public void MarkRead() { IsRead = true; Touch(); }
}

/// <summary>
/// Audit log ghi lại mọi event đến service này - giúp debug khi cần trace
/// "tại sao user nhận được notification này" hoặc "event nào đã trigger gì".
/// Payload lưu dạng JSON (jsonb trong Postgres) để query flexible.
/// </summary>
public class EventLog : BaseEntity
{
    // Tên event: "UserRegisteredEvent", "OrderCreatedEvent"...
    public string EventName { get; private set; } = default!;

    // JSON payload của event gốc - lưu nguyên để replay nếu cần
    public string Payload { get; private set; } = default!;

    // UserId hoặc SystemId của người trigger event - null nếu là system event
    public string? OperatorId { get; private set; }

    private EventLog() { }

    public static EventLog Create(string eventName, string payload, string? operatorId = null)
        => new() { EventName = eventName, Payload = payload, OperatorId = operatorId };
}
