using Cartiva.Domain;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cartiva.Infrastructure.Notifications.Channels;

public class SmsNotificationChannel : INotificationChannel
{
    private readonly ILogger<SmsNotificationChannel> _logger;

    public SmsNotificationChannel(ILogger<SmsNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "SMS channel not yet implemented. Notification {NotificationId} for {Recipient} skipped.",
            notification.Id,
            notification.Recipient);

        // TODO: Implement SMS provider integration (e.g., Twilio, AWS SNS, Azure Communication Services)
        // 1. Create ISmsProvider interface
        // 2. Implement provider-specific sender
        // 3. Add retry policy with Polly
        // 4. Add SMS template support
        // 5. Configure SMS settings in appsettings

        return Task.FromResult(false);
    }
}
