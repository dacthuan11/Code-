using MassTransit;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

/// <summary>
/// Notify user khi thanh toán đơn hàng thành công.
/// </summary>
public class OrderPaidConsumer : IConsumer<OrderPaidEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IPushSender _pushSender;

    public OrderPaidConsumer(INotificationRepository notifications, IPushSender pushSender)
    {
        _notifications = notifications;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<OrderPaidEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        var notification = Notification.Create(
            evt.UserId,
            "ORDER_PAID",
            "Thanh toán thành công ✓",
            $"Đơn hàng #{evt.OrderId:N} đã được thanh toán thành công. Số tiền: {evt.Amount:N0} VNĐ. Chúng tôi đang chuẩn bị hàng cho bạn.",
            NotificationChannel.Push);

        await _notifications.AddAsync(notification, ct);
        try
        {
            await _pushSender.SendAsync(evt.UserId, notification.Title, notification.Body, ct);
            notification.MarkSent();
        }
        catch (Exception ex) { notification.MarkFailed(ex.Message); }

        await _notifications.SaveChangesAsync(ct);
    }
}
