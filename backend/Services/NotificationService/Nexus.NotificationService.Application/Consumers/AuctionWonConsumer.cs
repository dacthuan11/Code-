using MassTransit;
using Nexus.NotificationService.Application.Commons;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class AuctionWonConsumer : IConsumer<AuctionWonEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;
    private const string AppUrl = "http://localhost:5173";

    public AuctionWonConsumer(
        INotificationRepository notifications,
        IEmailSender emailSender,
        IPushSender pushSender)
    {
        _notifications = notifications;
        _emailSender = emailSender;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<AuctionWonEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        // In-app push
        var push = Notification.Create(
            evt.WinnerId,
            "AUCTION_WON",
            "Chúc mừng! Bạn đã thắng đấu giá!",
            $"Bạn đã thắng phiên đấu giá với giá {evt.FinalPrice:N0} VNĐ. Vui lòng thanh toán trong 24h.",
            NotificationChannel.Push);

        await _notifications.AddAsync(push, ct);
        try
        {
            await _pushSender.SendAsync(evt.WinnerId, push.Title, push.Body, ct);
            push.MarkSent();
        }
        catch (Exception ex) { push.MarkFailed(ex.Message); }

        // Email thắng đấu giá
        if (!string.IsNullOrEmpty(evt.WinnerEmail))
        {
            var html = NexusEmailTemplate.AuctionWon(
                recipientName: "bạn",
                auctionId: evt.AuctionId,
                winningPrice: evt.FinalPrice,
                productTitle: $"Phiên đấu giá #{evt.AuctionId.ToString().ToUpper()[..8]}",
                appUrl: AppUrl);

            var email = Notification.Create(
                evt.WinnerId, "AUCTION_WON_EMAIL",
                "Bạn đã thắng đấu giá — Nexus",
                html, NotificationChannel.Email);

            await _notifications.AddAsync(email, ct);
            try
            {
                await _emailSender.SendAsync(evt.WinnerEmail, email.Title, html, ct);
                email.MarkSent();
            }
            catch (Exception ex) { email.MarkFailed(ex.Message); }
        }

        await _notifications.SaveChangesAsync(ct);
    }
}
