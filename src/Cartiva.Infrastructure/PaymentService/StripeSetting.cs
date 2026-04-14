using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Infrastructure.PaymentService
{
    public class StripeSettings
    {
        public string? SecretKey { get; set; }
        public string? PublishableKey { get; set; }
        public string? WebhookSecret { get; set; }
    }


  
}