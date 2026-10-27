using MassTransit;
using Nexus.NotificationService.Application.Commons;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class ShipmentDeliveredConsumer : IConsumer<ShipmentDeliveredEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;
    private const string AppUrl = "http://localhost:5173";

    public ShipmentDeliveredConsumer(
        INotificationRepository notifications,
        IEmailSender emailSender,
        IPushSender pushSender)
    {
        _notifications = notifications;
        _emailSender = emailSender;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        // In-app push notification
        var push = Notification.Create(
            evt.BuyerId,
            "SHIPMENT_DELIVERED",
            "Đơn hàng đã giao thành công",
            $"Đơn #{evt.OrderId:N} đã được giao thành công! Cảm ơn bạn đã mua hàng tại Nexus. Đừng quên đánh giá sản phẩm nhé.",
            NotificationChannel.Push);

        await _notifications.AddAsync(push, ct);
        try
        {
            if (evt.BuyerId != Guid.Empty)
                await _pushSender.SendAsync(evt.BuyerId, push.Title, push.Body, ct);
            push.MarkSent();
        }
        catch (Exception ex) { push.MarkFailed(ex.Message); }

        // Email xác nhận giao thành công
        if (!string.IsNullOrEmpty(evt.BuyerEmail))
        {
            var html = NexusEmailTemplate.ShipmentTracking(
                recipientName: string.IsNullOrEmpty(evt.RecipientName) ? "bạn" : evt.RecipientName,
                orderId: evt.OrderId,
                trackingNumber: evt.TrackingNumber,
                status: "Đã giao thành công — cảm ơn bạn đã mua hàng tại Nexus!",
                estimatedDelivery: null,
                appUrl: AppUrl);

            var email = Notification.Create(
                evt.BuyerId, "SHIPMENT_DELIVERED_EMAIL",
                "Đơn hàng đã giao thành công — Nexus",
                html, NotificationChannel.Email);

            await _notifications.AddAsync(email, ct);
            try
            {
                await _emailSender.SendAsync(evt.BuyerEmail, email.Title, html, ct);
                email.MarkSent();
            }
            catch (Exception ex) { email.MarkFailed(ex.Message); }
        }

        await _notifications.SaveChangesAsync(ct);
    }
}
