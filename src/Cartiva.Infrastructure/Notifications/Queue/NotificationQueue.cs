using System.Threading.Channels;
using Cartiva.Infrastructure.Notifications.Interfaces;

namespace Cartiva.Infrastructure.Notifications.Queue;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<int> _channel;

    public NotificationQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _channel = Channel.CreateBounded<int>(options);
    }

    public async Task EnqueueAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(notificationId, cancellationToken);
    }

    public async Task<int?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_channel.Reader.TryRead(out var notificationId))
            {
                return notificationId;
            }
        }

        return null;
    }
}
