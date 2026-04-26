using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class InvoiceStatusExtensions
{
    public static string ToValue(this InvoiceStatus status) => status switch
    {
        InvoiceStatus.PartiallyPaid => "PartiallyPaid",
        _ => status.ToString()
    };

    public static InvoiceStatus FromValue(string value) => value switch
    {
        "PartiallyPaid" => InvoiceStatus.PartiallyPaid,
        _ => Enum.Parse<InvoiceStatus>(value, true)
    };
}
