namespace Cartiva.Domain
{
    public enum PaymentMethod
    {
        Unknown = 0,
        BankTransfer = 1,    // Norwegian bank (KID)
        Card = 2,            // Stripe
        Vipps = 3,           // Norwegian mobile payment
        Klarna = 4,          // Buy now, pay later
        Cash = 5,            // Store pickup
        CreditNote = 6       // Applied credit
    }
}
