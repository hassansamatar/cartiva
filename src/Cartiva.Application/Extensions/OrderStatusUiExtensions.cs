using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class OrderStatusUiExtensions
{
    public static string GetBadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Approved => "bg-success",
        OrderStatus.Processing => "bg-info",
        OrderStatus.AwaitingShipmentApproval => "bg-info text-white",
        OrderStatus.Shipped => "bg-primary",
        OrderStatus.OutForDelivery => "bg-info text-white",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        OrderStatus.Refunded => "bg-secondary",
        OrderStatus.Completed => "bg-success",
        _ => "bg-secondary"
    };

    public static string GetIcon(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bi-hourglass",
        OrderStatus.Approved => "bi-check-circle",
        OrderStatus.Processing => "bi-gear",
        OrderStatus.AwaitingShipmentApproval => "bi-clock-history",
        OrderStatus.Shipped => "bi-box-seam",
        OrderStatus.OutForDelivery => "bi-truck",
        OrderStatus.Delivered => "bi-check-circle-fill",
        OrderStatus.Cancelled => "bi-x-circle",
        OrderStatus.Refunded => "bi-arrow-return-left",
        OrderStatus.Completed => "bi-star",
        _ => "bi-question-circle"
    };

    public static string GetTrackingMessage(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Awaiting payment confirmation. Complete payment to start processing.",
        OrderStatus.Approved => "Payment confirmed! We're preparing your order for shipment.",
        OrderStatus.Processing => "Your order is being processed and packed.",
        OrderStatus.AwaitingShipmentApproval => "Your order is waiting for shipment approval. We'll notify you soon.",
        OrderStatus.Shipped => "Your order has been shipped! Use tracking number to follow your package.",
        OrderStatus.OutForDelivery => "Your order is out for delivery today! Expect it soon.",
        OrderStatus.Delivered => "Your order has been delivered. Thank you for shopping with us!",
        OrderStatus.Cancelled => "This order has been cancelled. Contact support if you have questions.",
        OrderStatus.Refunded => "This order has been refunded. Funds should return within 3-5 business days.",
        OrderStatus.Completed => "Order completed. Thank you for your business!",
        _ => "Your order is being processed."
    };

    public static int GetProgress(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => 10,
        OrderStatus.Approved => 25,
        OrderStatus.Processing => 40,
        OrderStatus.AwaitingShipmentApproval => 45,
        OrderStatus.Shipped => 60,
        OrderStatus.OutForDelivery => 80,
        OrderStatus.Delivered => 100,
        OrderStatus.Cancelled => 0,
        OrderStatus.Refunded => 0,
        _ => 0
    };

    public static int GetEstimatedDeliveryDays(this OrderStatus status, DateTime orderDate) => status switch
    {
        OrderStatus.Pending => 7,
        OrderStatus.Approved => 6,
        OrderStatus.Processing => 5,
        OrderStatus.AwaitingShipmentApproval => 5,
        OrderStatus.Shipped => 3,
        OrderStatus.OutForDelivery => 1,
        OrderStatus.Delivered => 0,
        _ => 5
    };

    public static string GetProgressBarColor(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning",
        OrderStatus.Approved => "bg-primary",
        OrderStatus.Processing => "bg-info",
        OrderStatus.AwaitingShipmentApproval => "bg-info",
        OrderStatus.Shipped => "bg-primary",
        OrderStatus.OutForDelivery => "bg-info",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        OrderStatus.Refunded => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string GetIconBackground(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning bg-opacity-25",
        OrderStatus.Approved => "bg-success bg-opacity-25",
        OrderStatus.Processing => "bg-info bg-opacity-25",
        OrderStatus.AwaitingShipmentApproval => "bg-info bg-opacity-25",
        OrderStatus.Shipped => "bg-primary bg-opacity-25",
        OrderStatus.OutForDelivery => "bg-info bg-opacity-25",
        OrderStatus.Delivered => "bg-success bg-opacity-25",
        OrderStatus.Cancelled => "bg-danger bg-opacity-25",
        OrderStatus.Refunded => "bg-secondary bg-opacity-25",
        _ => "bg-secondary bg-opacity-25"
    };
}
