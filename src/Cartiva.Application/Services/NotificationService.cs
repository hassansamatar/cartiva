using System.Text.Json;
using Cartiva.Application.Interfaces;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationQueue _queue;
    private readonly IChannelResolver _channelResolver;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        INotificationQueue queue,
        IChannelResolver channelResolver,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _queue = queue;
        _channelResolver = channelResolver;
        _logger = logger;
    }

    public async Task<int> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve channel
            var channel = _channelResolver.ResolveChannel(request.Type, request.Channel);

            // Create notification entity
            var notification = new Notification
            {
                Type = request.Type,
                Channel = channel,
                Status = NotificationStatus.Pending,
                Recipient = request.Recipient,
                Subject = request.Subject,
                TemplateData = request.TemplateData != null 
                    ? JsonSerializer.Serialize(request.TemplateData) 
                    : null,
                UserId = request.UserId,
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            // Save to database
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification {NotificationId} created for {Recipient} via {Channel}",
                notification.Id, request.Recipient, channel);

            // Enqueue for background processing (fire-and-forget)
            await _queue.EnqueueAsync(notification.Id, cancellationToken);

            return notification.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for {Recipient}", request.Recipient);
            throw;
        }
    }

    public async Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetFailedNotificationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Failed)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task RetryFailedAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found for retry", notificationId);
            return;
        }

        if (notification.Status != NotificationStatus.Failed)
        {
            _logger.LogWarning(
                "Notification {NotificationId} is in {Status} status, cannot retry",
                notificationId, notification.Status);
            return;
        }

        // Reset status to pending
        notification.Status = NotificationStatus.Pending;
        notification.ErrorMessage = null;
        await _context.SaveChangesAsync(cancellationToken);

        // Re-enqueue
        await _queue.EnqueueAsync(notificationId, cancellationToken);

        _logger.LogInformation("Notification {NotificationId} queued for retry", notificationId);
    }
}
