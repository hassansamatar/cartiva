namespace Cartiva.Domain.Enums;

public enum NotificationType
{
    OrderConfirmation = 1,
    OrderShipped = 2,
    OrderDelivered = 3,
    OrderCancelled = 4,
    PaymentReceived = 5,
    PaymentFailed = 6,
    PasswordReset = 7,
    EmailVerification = 8,
    WelcomeEmail = 9,
    InvoiceGenerated = 10,
    ReturnRequestReceived = 11,
    ReturnRequestApproved = 12,
    ReturnRequestRejected = 13,
    PromotionalEmail = 14,
    AccountUpdated = 15,
    Custom = 99
}
