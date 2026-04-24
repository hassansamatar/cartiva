using Cartiva.Domain.Enums;

namespace Cartiva.Infrastructure.Templates.Models;

public class GenericNotificationModel
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? UserId { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
}
