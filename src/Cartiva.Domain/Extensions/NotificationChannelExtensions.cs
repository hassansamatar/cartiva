using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class NotificationChannelExtensions
{
    public static string ToValue(this NotificationChannel channel) => channel.ToString();

    public static NotificationChannel FromValue(string value) => value switch
    {
        _ => Enum.Parse<NotificationChannel>(value, true)
    };
}
