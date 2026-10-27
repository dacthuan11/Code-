using MassTransit;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

/// <summary>
/// Consumer xử lý BidPlacedEvent - confirm cho bidder rằng bid của họ đã được ghi nhận.
/// Gửi Push vì auction là real-time, user cần feedback ngay lập tức.
/// </summary>
public class BidPlacedConsumer : IConsumer<BidPlacedEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IPushSender _pushSender;

    public BidPlacedConsumer(INotificationRepository notifications, IPushSender pushSender)
    {
        _notifications = notifications;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<BidPlacedEvent> context)
    {
        var evt = context.Message;

        // Notify người đặt bid rằng bid của họ đã thành công
        var notification = Notification.Create(
            evt.BidderId,
            "BID_PLACED",
            "Bid Confirmed",
            $"Your bid of {evt.BidAmount:C} on auction {evt.AuctionId} has been placed.",
            NotificationChannel.Push);

        await _notifications.AddAsync(notification, context.CancellationToken);

        try
        {
            await _pushSender.SendAsync(evt.BidderId, notification.Title, notification.Body, context.CancellationToken);
            notification.MarkSent();
        }
        catch (Exception ex)
        {
            notification.MarkFailed(ex.Message);
        }

        await _notifications.SaveChangesAsync(context.CancellationToken);
    }
}

/// <summary>
/// Consumer xử lý OutbidEvent - thông báo cho người bị vượt giá.
/// Đây là thông báo quan trọng với user: họ có thể muốn đặt giá cao hơn ngay.
/// Latency thấp quan trọng ở đây - push phù hợp hơn email.
/// </summary>
public class OutbidConsumer : IConsumer<OutbidEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IPushSender _pushSender;

    public OutbidConsumer(INotificationRepository notifications, IPushSender pushSender)
    {
        _notifications = notifications;
        _pushSender = pushSender;
    }

    public async Task Consume(ConsumeContext<OutbidEvent> context)
    {
        var evt = context.Message;

        // Notify người bị outbid - dùng PreviousBidderId vì họ là người cần biết
        var notification = Notification.Create(
            evt.PreviousBidderId,
            "OUTBID",
            "You've been outbid!",
            $"Someone placed a higher bid. New bid: {evt.NewAmount:C}",
            NotificationChannel.Push);

        await _notifications.AddAsync(notification, context.CancellationToken);

        try
        {
            await _pushSender.SendAsync(evt.PreviousBidderId, notification.Title, notification.Body, context.CancellationToken);
            notification.MarkSent();
        }
        catch (Exception ex)
        {
            notification.MarkFailed(ex.Message);
        }

        await _notifications.SaveChangesAsync(context.CancellationToken);
    }
}
