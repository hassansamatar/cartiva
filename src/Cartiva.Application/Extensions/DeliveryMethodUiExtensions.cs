using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class DeliveryMethodUiExtensions
{
    public static string GetEstimate(this DeliveryMethod deliveryMethod) => deliveryMethod switch
    {
        DeliveryMethod.Standard => "3-5 business days",
        DeliveryMethod.Express => "1-2 business days",
        DeliveryMethod.NextDay => "Next business day",
        DeliveryMethod.Pickup => "Ready in 2 hours",
        _ => "3-5 business days"
    };
}
