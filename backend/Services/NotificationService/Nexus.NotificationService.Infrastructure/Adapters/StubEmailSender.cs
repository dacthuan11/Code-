using Microsoft.Extensions.Logging;
using Nexus.NotificationService.Domain.Repositories;

namespace Nexus.NotificationService.Infrastructure.Adapters;

/// <summary>
/// Stub implementation của IEmailSender - không gửi email thật, chỉ log ra console.
/// Dùng cho dev/test để không cần cấu hình SMTP hay SendGrid API key.
/// Production: thay bằng SendGridEmailSender hoặc SesEmailSender.
/// </summary>
public class StubEmailSender : IEmailSender
{
    private readonly ILogger<StubEmailSender> _logger;
    public StubEmailSender(ILogger<StubEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        // Log ra console để dev verify email content mà không cần inbox thật
        _logger.LogInformation("[StubEmail] To: {Email} | Subject: {Subject}", toEmail, subject);
        return Task.CompletedTask; // Giả vờ thành công luôn
    }
}

/// <summary>
/// Stub implementation của IPushSender - không gửi push thật, chỉ log ra console.
/// Production: thay bằng FirebasePushSender (FCM) hoặc OneSignalPushSender.
/// Nhớ update DI registration trong Program.cs khi swap.
/// </summary>
public class StubPushSender : IPushSender
{
    private readonly ILogger<StubPushSender> _logger;
    public StubPushSender(ILogger<StubPushSender> logger) => _logger = logger;

    public Task SendAsync(Guid userId, string title, string body, CancellationToken ct = default)
    {
        // Log UserId thay vì device token vì Stub không track device token
        _logger.LogInformation("[StubPush] To: {UserId} | Title: {Title}", userId, title);
        return Task.CompletedTask;
    }
}
