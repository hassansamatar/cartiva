using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ApplicationUtility
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Original method (for simple text emails)
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailWithInlineImageAsync(email, subject, htmlMessage, null);
        }

        // New method that supports inline image attachment
        public async Task SendEmailWithInlineImageAsync(string email, string subject, string htmlMessage, byte[] qrCodeBytes)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var password = _configuration["EmailSettings:Password"];
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            System.IO.MemoryStream stream = null;
            try
            {
                if (qrCodeBytes != null && qrCodeBytes.Length > 0)
                {
                    stream = new System.IO.MemoryStream(qrCodeBytes);
                    var inlineAttachment = new Attachment(stream, "qr.png", "image/png");
                    inlineAttachment.ContentId = "qrCode";
                    inlineAttachment.ContentDisposition.Inline = true;
                    inlineAttachment.ContentDisposition.DispositionType = "inline";
                    mailMessage.Attachments.Add(inlineAttachment);
                }

                await client.SendMailAsync(mailMessage);
            }
            finally
            {
                // Ensure the stream is disposed after sending
                stream?.Dispose();
            }
        }
    }
}