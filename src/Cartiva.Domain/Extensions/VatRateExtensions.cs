using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class VatRateExtensions
{
    public static string ToValue(this VatRate rate) => ((int)rate).ToString();

    public static VatRate FromValue(string value) => value switch
    {
        _ => Enum.Parse<VatRate>(value, true)
    };
}
