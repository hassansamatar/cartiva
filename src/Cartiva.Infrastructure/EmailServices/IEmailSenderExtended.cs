using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Infrastructure.EmailServices
{
    public interface IEmailSenderExtended : IEmailSender
    {
        Task SendEmailWithInlineImageAsync(string email, string subject, string htmlMessage, byte[] qrCodeBytes);
    }
}
