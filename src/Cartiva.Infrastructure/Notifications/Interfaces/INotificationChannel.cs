using Cartiva.Domain;

namespace Cartiva.Infrastructure.Notifications.Interfaces;

public interface INotificationChannel
{
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
