using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class DeliveryMethodExtensions
{
    public static string ToValue(this DeliveryMethod method) => method switch
    {
        DeliveryMethod.Standard => "Standard (3-5 days)",
        DeliveryMethod.Express => "Express (1-2 days)",
        DeliveryMethod.NextDay => "Next Day",
        DeliveryMethod.Pickup => "Store Pickup",
        _ => method.ToString()
    };

    public static DeliveryMethod FromValue(string value) => value switch
    {
        "Standard (3-5 days)" => DeliveryMethod.Standard,
        "Express (1-2 days)" => DeliveryMethod.Express,
        "Next Day" => DeliveryMethod.NextDay,
        "Store Pickup" => DeliveryMethod.Pickup,
        _ => Enum.Parse<DeliveryMethod>(value, true)
    };
}
