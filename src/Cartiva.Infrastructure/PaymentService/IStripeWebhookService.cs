using System.Threading.Tasks;
using Stripe;

namespace Cartiva.Infrastructure.PaymentService
{
    public interface IStripeWebhookService
    {
        Task ProcessEventAsync(Event stripeEvent);
    }
}
