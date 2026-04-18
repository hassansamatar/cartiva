using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Cartiva.Infrastructure.PaymentService;
using Hangfire;

namespace cartivaWeb.Controllers
{
    [ApiController]
    [Route("api/webhooks/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;
        private readonly IStripeWebhookService _webhookService;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(IOptions<StripeSettings> stripeSettings, IStripeWebhookService webhookService, ILogger<StripeWebhookController> logger)
        {
            _stripeSettings = stripeSettings.Value;
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];
            Event stripeEvent;
            try
            {
                // Fixed: added throwOnApiVersionMismatch: false to accept CLI's API version
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSettings.WebhookSecret, throwOnApiVersionMismatch: false);
            }
            catch (StripeException e)
            {
                _logger.LogWarning(e, "Stripe webhook signature validation failed.");
                return BadRequest();
            }

            // Enqueue processing to Hangfire for scalability
            BackgroundJob.Enqueue(() => _webhookService.ProcessEventAsync(stripeEvent));
            // Optional: remove or comment out this line after debugging – it logs the secret!
            
            return Ok();
        }
    }
}