using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class OrderStatusExtensions
{
    public static string ToValue(this OrderStatus status) => status switch
    {
        OrderStatus.AwaitingShipmentApproval => "Awaiting Shipment Approval",
        OrderStatus.OutForDelivery => "Out for Delivery",
        _ => status.ToString()
    };

    public static OrderStatus FromValue(string value) => value switch
    {
        "Awaiting Shipment Approval" => OrderStatus.AwaitingShipmentApproval,
        "Out for Delivery" => OrderStatus.OutForDelivery,
        _ => Enum.Parse<OrderStatus>(value, true)
    };
}
