using MassTransit;
using Nexus.NotificationService.Application.Commons;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;
    private const string AppUrl = "http://localhost:5173";

    public ShipmentCreatedConsumer(
        INotificationRepository notifications,
        IEmailSender emailSender,
        IPushSender pushSender)
    {
        _notifications = notifications;
        _emailSender = emailSender;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        var estimatedStr = evt.ExpectedDelivery.HasValue
            ? evt.ExpectedDelivery.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : null;

        // In-app push notification
        var push = Notification.Create(
            evt.BuyerId,
            "SHIPMENT_CREATED",
            "Đơn hàng đang được giao",
            $"Đơn #{evt.OrderId:N[..8]} đã bàn giao cho {evt.Carrier}. Mã vận đơn: {evt.TrackingNumber}.",
            NotificationChannel.Push);

        await _notifications.AddAsync(push, ct);
        if (evt.BuyerId != Guid.Empty)
        {
            try
            {
                await _pushSender.SendAsync(evt.BuyerId, push.Title, push.Body, ct);
                push.MarkSent();
            }
            catch (Exception ex) { push.MarkFailed(ex.Message); }
        }

        // Email xác nhận giao hàng
        if (!string.IsNullOrEmpty(evt.BuyerEmail))
        {
            var html = NexusEmailTemplate.ShipmentTracking(
                recipientName: string.IsNullOrEmpty(evt.RecipientName) ? "bạn" : evt.RecipientName,
                orderId: evt.OrderId,
                trackingNumber: evt.TrackingNumber,
                status: $"Đã bàn giao cho {evt.Carrier} — đang trên đường vận chuyển",
                estimatedDelivery: estimatedStr,
                appUrl: AppUrl);

            var email = Notification.Create(
                evt.BuyerId, "SHIPMENT_CREATED_EMAIL",
                "Đơn hàng của bạn đang được giao — Nexus",
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
