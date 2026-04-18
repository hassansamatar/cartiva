using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stripe;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cartiva.Infrastructure.PaymentService
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<StripeWebhookService> _logger;

        public StripeWebhookService(ApplicationDbContext db, ILogger<StripeWebhookService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task ProcessEventAsync(Event stripeEvent)
        {
            _logger.LogInformation("[StripeWebhook] Processing Stripe event: {Type}", stripeEvent.Type);
            _logger.LogInformation("[StripeWebhook] Event Id: {Id}", stripeEvent.Id);

            // Idempotency: check if event already processed
            var alreadyProcessed = await _db.ProcessedStripeEvents.AnyAsync(e => e.EventId == stripeEvent.Id);
            if (alreadyProcessed)
            {
                _logger.LogWarning("[StripeWebhook] Duplicate event received: {Id}", stripeEvent.Id);
                return;
            }

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    var paymentIntent = (stripeEvent.Data.Object as PaymentIntent);
                    _logger.LogInformation("[StripeWebhook] PaymentIntent succeeded: {PaymentIntentId}", paymentIntent?.Id);
                    // TODO: Update order/payment status in DB
                    break;
                case "payment_intent.payment_failed":
                    var failedIntent = (stripeEvent.Data.Object as PaymentIntent);
                    _logger.LogInformation("[StripeWebhook] PaymentIntent failed: {PaymentIntentId}", failedIntent?.Id);
                    // TODO: Handle failed payment
                    break;
                default:
                    _logger.LogInformation("[StripeWebhook] Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }

            // Mark event as processed
            _db.ProcessedStripeEvents.Add(new Cartiva.Domain.ProcessedStripeEvent { EventId = stripeEvent.Id });
            await _db.SaveChangesAsync();
        }
    }
}
