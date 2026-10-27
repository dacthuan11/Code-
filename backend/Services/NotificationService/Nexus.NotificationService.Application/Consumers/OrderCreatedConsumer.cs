using MassTransit;
using Nexus.NotificationService.Application.Commons;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;
    private const string AppUrl = "http://localhost:5173"; // TODO: lấy từ config

    public OrderCreatedConsumer(INotificationRepository notifications, IEmailSender emailSender, IPushSender pushSender)
    {
        _notifications = notifications; _emailSender = emailSender; _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        // In-app push
        var push = Notification.Create(
            evt.UserId, "ORDER_CREATED",
            "✅ Đặt hàng thành công!",
            $"Đơn hàng của bạn đã được ghi nhận. Tổng tiền: {evt.TotalAmount:N0} VNĐ.",
            NotificationChannel.Push);

        await _notifications.AddAsync(push, ct);
        try { await _pushSender.SendAsync(evt.UserId, push.Title, push.Body, ct); push.MarkSent(); }
        catch (Exception ex) { push.MarkFailed(ex.Message); }

        // Email xác nhận - dùng Nexus template
        if (!string.IsNullOrEmpty(evt.UserEmail))
        {
            var html = NexusEmailTemplate.OrderConfirmation(
                recipientName: "Quý khách",
                orderId: evt.OrderId,
                total: evt.TotalAmount,
                shippingFee: 0,       // TODO: truyền qua event
                address: "Xem trong ứng dụng",
                estimatedDelivery: null,
                deliveryTypeLabel: "Giao hàng tiêu chuẩn",
                appUrl: AppUrl);

            var email = Notification.Create(
                evt.UserId, "ORDER_CREATED_EMAIL",
                "✅ Xác nhận đơn hàng — Nexus Commerce",
                html, NotificationChannel.Email);

            await _notifications.AddAsync(email, ct);
            try { await _emailSender.SendAsync(evt.UserEmail, email.Title, html, ct); email.MarkSent(); }
            catch (Exception ex) { email.MarkFailed(ex.Message); }
        }

        await _notifications.SaveChangesAsync(ct);
    }
}
