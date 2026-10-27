using MassTransit;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class AuctionFailedConsumer : IConsumer<AuctionFailedEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;

    public AuctionFailedConsumer(INotificationRepository notifications, IEmailSender emailSender, IPushSender pushSender)
    {
        _notifications = notifications;
        _emailSender = emailSender;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<AuctionFailedEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        // Chỉ tạo notification nếu biết SellerId
        if (evt.SellerId != Guid.Empty)
        {
            var push = Notification.Create(
                evt.SellerId,
                "AUCTION_FAILED",
                "Phiên đấu giá kết thúc không thành công",
                $"Phiên đấu giá #{evt.AuctionId} đã kết thúc mà không tìm được người thắng. Lý do: {evt.Reason}.",
                NotificationChannel.Push);

            await _notifications.AddAsync(push, ct);
            try
            {
                await _pushSender.SendAsync(evt.SellerId, push.Title, push.Body, ct);
                push.MarkSent();
            }
            catch (Exception ex) { push.MarkFailed(ex.Message); }
        }

        // Email thông báo cho seller nếu có email
        if (!string.IsNullOrEmpty(evt.SellerEmail))
        {
            var emailBody = $@"
<div style='font-family:sans-serif;max-width:600px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:8px'>
  <h2 style='color:#dc2626'>Phiên đấu giá kết thúc không thành công</h2>
  <p>Xin chào,</p>
  <p>Phiên đấu giá <strong>#{evt.AuctionId}</strong> của bạn đã kết thúc mà không có người thắng.</p>
  <p><strong>Lý do:</strong> {evt.Reason}</p>
  <p>Bạn có thể tạo lại phiên đấu giá mới trên <strong>ProjectNexus</strong> với mức giá hoặc thời gian khác.</p>
  <hr style='border:none;border-top:1px solid #e5e7eb;margin:20px 0'>
  <p style='color:#9ca3af;font-size:12px'>ProjectNexus — Nền tảng thương mại điện tử</p>
</div>";

            var notifId = evt.SellerId != Guid.Empty ? evt.SellerId : Guid.NewGuid();
            var email = Notification.Create(
                notifId, "AUCTION_FAILED_EMAIL",
                "Phiên đấu giá của bạn đã kết thúc — ProjectNexus",
                emailBody, NotificationChannel.Email);

            await _notifications.AddAsync(email, ct);
            try
            {
                await _emailSender.SendAsync(evt.SellerEmail, email.Title, emailBody, ct);
                email.MarkSent();
            }
            catch (Exception ex) { email.MarkFailed(ex.Message); }
        }

        await _notifications.SaveChangesAsync(ct);
    }
}
