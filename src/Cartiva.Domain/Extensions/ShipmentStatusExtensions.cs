using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class ShipmentStatusExtensions
{
    public static string ToValue(this ShipmentStatus status) => status switch
    {
        ShipmentStatus.PendingApproval => "Pending Approval",
        _ => status.ToString()
    };

    public static ShipmentStatus FromValue(string value) => value switch
    {
        "Pending Approval" => ShipmentStatus.PendingApproval,
        _ => Enum.Parse<ShipmentStatus>(value, true)
    };
}
