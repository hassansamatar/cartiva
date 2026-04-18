namespace Cartiva.Domain
{
    public class ProcessedStripeEvent
    {
        public int Id { get; set; }
        public string EventId { get; set; } = null!;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
