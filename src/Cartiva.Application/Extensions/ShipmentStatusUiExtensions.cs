using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class ShipmentStatusUiExtensions
{
    public static string GetBadgeClass(this ShipmentStatus status) => status switch
    {
        ShipmentStatus.PendingApproval => "bg-warning text-dark",
        ShipmentStatus.Approved => "bg-success",
        ShipmentStatus.Shipped => "bg-primary",
        ShipmentStatus.Delivered => "bg-success",
        ShipmentStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public static string GetIcon(this ShipmentStatus status) => status switch
    {
        ShipmentStatus.PendingApproval => "bi-hourglass",
        ShipmentStatus.Approved => "bi-check-circle",
        ShipmentStatus.Shipped => "bi-box-seam",
        ShipmentStatus.Delivered => "bi-check-circle-fill",
        ShipmentStatus.Cancelled => "bi-x-circle",
        _ => "bi-question-circle"
    };
}
