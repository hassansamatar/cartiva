using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class NotificationTypeExtensions
{
    public static string ToValue(this NotificationType type) => type.ToString();

    public static NotificationType FromValue(string value) => value switch
    {
        _ => Enum.Parse<NotificationType>(value, true)
    };
}
