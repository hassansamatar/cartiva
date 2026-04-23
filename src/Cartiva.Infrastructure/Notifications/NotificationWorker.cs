using Cartiva.Domain.Enums;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cartiva.Infrastructure.Notifications;

public class NotificationWorker : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        INotificationQueue queue,
        IServiceProvider serviceProvider,
        ILogger<NotificationWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Dequeue notification ID
                var notificationId = await _queue.DequeueAsync(stoppingToken);

                if (notificationId.HasValue)
                {
                    await ProcessNotificationAsync(notificationId.Value, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification worker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Notification Worker stopped");
    }

    private async Task ProcessNotificationAsync(int notificationId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var channelResolver = scope.ServiceProvider.GetRequiredService<ChannelResolver>();

        try
        {
            // Load notification from database
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", notificationId);
                return;
            }

            // Skip if already processed
            if (notification.Status == NotificationStatus.Sent || 
                notification.Status == NotificationStatus.Cancelled)
            {
                _logger.LogInformation(
                    "Notification {NotificationId} already in {Status} status, skipping",
                    notificationId, notification.Status);
                return;
            }

            // Update status to processing
            notification.Status = NotificationStatus.Processing;
            notification.ProcessedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Processing notification {NotificationId} via {Channel}",
                notificationId, notification.Channel);

            // Resolve channel
            var channel = channelResolver.GetChannel(notification.Channel);

            if (channel == null)
            {
                throw new InvalidOperationException(
                    $"No channel implementation found for {notification.Channel}");
            }

            // Send via channel
            var success = await channel.SendAsync(notification, cancellationToken);

            if (success)
            {
                // Update status to sent
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.ErrorMessage = null;

                _logger.LogInformation(
                    "Notification {NotificationId} sent successfully",
                    notificationId);
            }
            else
            {
                // Update status to failed
                notification.Status = NotificationStatus.Failed;
                notification.RetryCount++;
                notification.ErrorMessage = "Failed to send notification";

                _logger.LogWarning(
                    "Notification {NotificationId} failed to send (attempt {RetryCount})",
                    notificationId, notification.RetryCount);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification {NotificationId}", notificationId);

            try
            {
                // Update notification status to failed
                var notification = await dbContext.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

                if (notification != null)
                {
                    notification.Status = NotificationStatus.Failed;
                    notification.RetryCount++;
                    notification.ErrorMessage = ex.Message;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update notification {NotificationId} status", notificationId);
            }
        }
    }
}
