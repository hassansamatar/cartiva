using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class ShippingCarrierExtensions
{
    public static string ToValue(this ShippingCarrier carrier) => carrier switch
    {
        ShippingCarrier.Posten => "Posten Norge",
        ShippingCarrier.Helthjem => "Helthjem",
        ShippingCarrier.Bring => "Bring",
        ShippingCarrier.DHL => "DHL Express",
        _ => carrier.ToString()
    };

    public static ShippingCarrier FromValue(string value) => value switch
    {
        "Posten Norge" => ShippingCarrier.Posten,
        "Helthjem" => ShippingCarrier.Helthjem,
        "Bring" => ShippingCarrier.Bring,
        "DHL Express" => ShippingCarrier.DHL,
        _ => Enum.Parse<ShippingCarrier>(value, true)
    };
}
