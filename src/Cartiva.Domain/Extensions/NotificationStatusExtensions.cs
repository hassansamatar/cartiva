using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class NotificationStatusExtensions
{
    public static string ToValue(this NotificationStatus status) => status.ToString();

    public static NotificationStatus FromValue(string value) => value switch
    {
        _ => Enum.Parse<NotificationStatus>(value, true)
    };
}
