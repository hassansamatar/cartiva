using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Interfaces;

public interface INotificationService
{
    Task<int> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Notification>> GetFailedNotificationsAsync(CancellationToken cancellationToken = default);

    Task RetryFailedAsync(int notificationId, CancellationToken cancellationToken = default);
}

public record NotificationRequest(
    string Recipient,
    NotificationType Type,
    Dictionary<string, object>? TemplateData = null,
    NotificationChannel? Channel = null,
    string? UserId = null,
    string? ReferenceId = null,
    string? ReferenceType = null,
    string? Subject = null
);
