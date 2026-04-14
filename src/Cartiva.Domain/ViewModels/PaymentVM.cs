
namespace Cartiva.Domain.ViewModels
{
    public class PaymentVM
    {
        public OrderHeader Order { get; set; }
        public string ClientSecret { get; set; }
        public string PublishableKey { get; set; }
        public string PaymentIntentId { get; set; }
    }
}