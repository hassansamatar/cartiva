using System.Net;
using System.Net.Mail;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cartiva.Infrastructure.Notifications.Channels;

public class SmtpEmailSender : ISmtpEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPassword = (_settings.Password ?? string.Empty).Replace(" ", string.Empty);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.SenderEmail, normalizedPassword),
                EnableSsl = _settings.EnableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}", to);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP authentication or transport failure for {Recipient}. Sender {SenderEmail} was rejected by {Server}:{Port}.", to, _settings.SenderEmail, _settings.SmtpServer, _settings.SmtpPort);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            return false;
        }
    }
}
