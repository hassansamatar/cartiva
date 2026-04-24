namespace Cartiva.Infrastructure.Notifications.Interfaces;

public interface INotificationQueue
{
    Task EnqueueAsync(int notificationId, CancellationToken cancellationToken = default);

    Task<int?> DequeueAsync(CancellationToken cancellationToken = default);
}
