namespace Cartiva.Infrastructure.Notifications.Interfaces;

public interface ISmtpEmailSender
{
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
