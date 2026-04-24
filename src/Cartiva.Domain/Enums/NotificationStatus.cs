namespace Cartiva.Domain.Enums;

public enum NotificationStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Retrying = 5,
    Cancelled = 6
}
