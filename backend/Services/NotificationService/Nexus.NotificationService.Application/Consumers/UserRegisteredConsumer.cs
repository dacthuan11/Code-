using MassTransit;
using Microsoft.Extensions.Configuration;
using Nexus.NotificationService.Application.Commons;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.Shared.Events;

namespace Nexus.NotificationService.Application.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly INotificationRepository _notifications;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public UserRegisteredConsumer(INotificationRepository notifications, IEmailSender emailSender, IConfiguration config)
    {
        _notifications = notifications;
        _emailSender = emailSender;
        _config = config;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var appUrl = _config["App:FrontendUrl"] ?? "http://localhost:3000";
        var verifyUrl = $"{appUrl}/verify-email?token={evt.VerificationToken}";
        var html = NexusEmailTemplate.VerifyEmail(evt.FullName, verifyUrl);

        var notification = Notification.Create(
            evt.UserId, "USER_REGISTERED",
            "Xác thực email tài khoản Nexus của bạn",
            html, NotificationChannel.Email);

        await _notifications.AddAsync(notification, ct);
        try { await _emailSender.SendAsync(evt.Email, notification.Title, html, ct); notification.MarkSent(); }
        catch (Exception ex) { notification.MarkFailed(ex.Message); }

        await _notifications.SaveChangesAsync(ct);
    }
}
