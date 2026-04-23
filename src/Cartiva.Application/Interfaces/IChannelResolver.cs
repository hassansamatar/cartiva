using Cartiva.Domain.Enums;

namespace Cartiva.Application.Interfaces;

public interface IChannelResolver
{
    NotificationChannel ResolveChannel(NotificationType type, NotificationChannel? preferredChannel = null);
}
