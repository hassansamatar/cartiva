using Cartiva.Domain.Enums;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cartiva.Infrastructure.Notifications;

public class ChannelResolver
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<ChannelResolver> _logger;

    public ChannelResolver(
        IEnumerable<INotificationChannel> channels,
        ILogger<ChannelResolver> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    public INotificationChannel? GetChannel(NotificationChannel channelType)
    {
        var channel = channelType switch
        {
            NotificationChannel.Email => _channels.FirstOrDefault(c => 
                c.GetType().Name.Contains("Email")),

            NotificationChannel.Sms => _channels.FirstOrDefault(c => 
                c.GetType().Name.Contains("Sms")),

            NotificationChannel.Push => _channels.FirstOrDefault(c => 
                c.GetType().Name.Contains("Push")),

            _ => null
        };

        if (channel == null)
        {
            _logger.LogWarning("No channel implementation found for {ChannelType}", channelType);
        }

        return channel;
    }
}
