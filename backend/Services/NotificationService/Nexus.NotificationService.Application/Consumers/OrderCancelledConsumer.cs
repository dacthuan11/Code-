using MassTransit;
using Nexus.NotificationService.Application.Commons;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;
    private const string AppUrl = "http://localhost:5173";

    public OrderCancelledConsumer(INotificationRepository notifications, IEmailSender emailSender, IPushSender pushSender)
    {
        _notifications = notifications; _emailSender = emailSender; _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        // In-app push
        var push = Notification.Create(
            evt.UserId, "ORDER_CANCELLED",
            "Đơn hàng đã bị hủy",
            $"Đơn hàng #{evt.OrderId.ToString().ToUpper()[..8]} đã hủy. Lý do: {evt.Reason}",
            NotificationChannel.Push);

        await _notifications.AddAsync(push, ct);
        try { await _pushSender.SendAsync(evt.UserId, push.Title, push.Body, ct); push.MarkSent(); }
        catch (Exception ex) { push.MarkFailed(ex.Message); }

        // Email hủy đơn với template
        if (!string.IsNullOrEmpty(evt.UserEmail))
        {
            var html = NexusEmailTemplate.OrderCancelled(
                recipientName: "Quý khách",
                orderId: evt.OrderId,
                reason: evt.Reason ?? "Không có lý do cụ thể",
                wasOnlinePaid: false,
                appUrl: AppUrl);

            var email = Notification.Create(
                evt.UserId, "ORDER_CANCELLED_EMAIL",
                "Thông báo hủy đơn hàng — Nexus Commerce",
                html, NotificationChannel.Email);

            await _notifications.AddAsync(email, ct);
            try { await _emailSender.SendAsync(evt.UserEmail, email.Title, html, ct); email.MarkSent(); }
            catch (Exception ex) { email.MarkFailed(ex.Message); }
        }

        await _notifications.SaveChangesAsync(ct);
    }
}
