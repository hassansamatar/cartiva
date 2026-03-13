using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyUtility
{
    public class EmailSender: IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Here you would implement the logic to send an email using your preferred email service.
            // For example, you could use SMTP, SendGrid, or any other email sending service.
            // This is a placeholder implementation. You should replace it with actual email sending code.
           
            return Task.CompletedTask;
        }
    }
}
