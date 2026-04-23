using Cartiva.Application.Interfaces;
using Cartiva.Domain.Enums;

namespace Cartiva.Application.Services;

public class ChannelResolver : IChannelResolver
{
    public NotificationChannel ResolveChannel(NotificationType type, NotificationChannel? preferredChannel = null)
    {
        // If caller specifies a channel, use it
        if (preferredChannel.HasValue)
        {
            return preferredChannel.Value;
        }

        // Default channel resolution logic based on notification type
        return type switch
        {
            NotificationType.OrderConfirmation => NotificationChannel.Email,
            NotificationType.OrderShipped => NotificationChannel.Email,
            NotificationType.OrderDelivered => NotificationChannel.Email,
            NotificationType.OrderCancelled => NotificationChannel.Email,
            NotificationType.PaymentReceived => NotificationChannel.Email,
            NotificationType.PaymentFailed => NotificationChannel.Email,
            NotificationType.PasswordReset => NotificationChannel.Email,
            NotificationType.EmailVerification => NotificationChannel.Email,
            NotificationType.WelcomeEmail => NotificationChannel.Email,
            NotificationType.InvoiceGenerated => NotificationChannel.Email,
            NotificationType.ReturnRequestReceived => NotificationChannel.Email,
            NotificationType.ReturnRequestApproved => NotificationChannel.Email,
            NotificationType.ReturnRequestRejected => NotificationChannel.Email,
            NotificationType.PromotionalEmail => NotificationChannel.Email,
            NotificationType.AccountUpdated => NotificationChannel.Email,
            NotificationType.Custom => NotificationChannel.Email,
            _ => NotificationChannel.Email
        };
    }
}
