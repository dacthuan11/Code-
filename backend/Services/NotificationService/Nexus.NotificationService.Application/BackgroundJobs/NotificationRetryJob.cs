using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.NotificationService.Domain.Entities;
using Nexus.NotificationService.Domain.Repositories;

namespace Nexus.NotificationService.Application.BackgroundJobs;

/// <summary>
/// Retry notifications thất bại mỗi 5 phút.
/// Pattern: query Pending/Failed notifications chưa vượt quá maxRetries, rồi thử gửi lại.
/// Spec: thử lại tối đa 3 lần trước khi bỏ qua hoàn toàn.
/// </summary>
public class NotificationRetryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationRetryJob> _logger;
    private const int MaxRetries = 3;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public NotificationRetryJob(IServiceScopeFactory scopeFactory, ILogger<NotificationRetryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationRetryJob started - retrying every {Interval}m", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            try
            {
                await RetryFailedNotificationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in NotificationRetryJob");
            }
        }
    }

    private async Task RetryFailedNotificationsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var pushSender = scope.ServiceProvider.GetRequiredService<IPushSender>();

        var pending = await repo.GetPendingRetriableAsync(MaxRetries, ct);
        if (pending.Count == 0) return;

        _logger.LogInformation("Retrying {Count} failed notifications", pending.Count);

        foreach (var notification in pending)
        {
            if (!notification.CanRetry(MaxRetries)) continue;

            try
            {
                if (notification.Channel == NotificationChannel.Email)
                    await emailSender.SendAsync($"user-{notification.UserId}@nexus.dev", notification.Title, notification.Body, ct);
                else
                    await pushSender.SendAsync(notification.UserId, notification.Title, notification.Body, ct);

                notification.MarkSent();
                _logger.LogInformation("Retry success: notification {Id} ({Type})", notification.Id, notification.Type);
            }
            catch (Exception ex)
            {
                notification.MarkFailed(ex.Message);
                _logger.LogWarning("Retry failed: notification {Id} (attempt {Retry}/{Max}): {Error}",
                    notification.Id, notification.RetryCount, MaxRetries, ex.Message);
            }

            await repo.UpdateAsync(notification, ct);
        }

        await repo.SaveChangesAsync(ct);
    }
}
