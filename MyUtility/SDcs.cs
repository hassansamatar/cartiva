using System;
using System.Collections.Generic;
using System.Text;

namespace MyUtility
{
    public static class SD
    {
        // ======================
        // ROLE CONSTANTS
        // ======================
        public const string Role_Customer = "Customer";
        public const string Role_Company = "Company";
        public const string Role_Admin = "Admin";
        public const string Role_Employee = "Employee";

        // ======================
        // ORDER STATUS CONSTANTS
        // ======================
        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusProcessing = "Processing";
        public const string StatusShipped = "Shipped";
        public const string StatusOutForDelivery = "Out for Delivery";
        public const string StatusDelivered = "Delivered";
        public const string StatusCancelled = "Cancelled";
        public const string StatusRefunded = "Refunded";
        public const string StatusCompleted = "Completed";

        // ======================
        // PAYMENT STATUS CONSTANTS
        // ======================
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDeferred = "Deferred";
        public const string PaymentStatusRejected = "Rejected";
        public const string PaymentStatusRefunded = "Refunded";

        // ======================
        // SIZE TYPE CONSTANTS
        // ======================
        public const string SizeTypeRegular = "Regular";
        public const string SizeTypeSuit = "Suit";
        public const string SizeTypeKid = "Kid";
        public const string SizeTypeShoe = "Shoe";

        // ======================
        // CART CONSTANTS
        // ======================
        public const string CartSessionKey = "SessionShoppingCart";

        // ======================
        // DELIVERY CONSTANTS
        // ======================
        public const string DeliveryStandard = "Standard (3-5 days)";
        public const string DeliveryExpress = "Express (1-2 days)";
        public const string DeliveryNextDay = "Next Day";
        public const string DeliveryPickup = "Store Pickup";

        // ======================
        // SHIPPING CARRIERS
        // ======================
        public const string CarrierPosten = "Posten Norge";
        public const string CarrierHelthjem = "Helthjem";
        public const string CarrierBring = "Bring";
        public const string CarrierDHL = "DHL Express";

        // ======================
        // QR CODE SETTINGS
        // ======================
        public const int QrCodeSize = 20;           // Size in pixels
        public const string QrCodeFormat = "png";
        public const int QrCodeErrorCorrection = 2; // Q level (0-3: L, M, Q, H)

        // ======================
        // ORDER TRACKING
        // ======================
        public static string GetOrderTrackingMessage(string status)
        {
            return status switch
            {
                StatusPending => "Awaiting payment confirmation. Complete payment to start processing.",
                StatusApproved => "Payment confirmed! We're preparing your order for shipment.",
                StatusProcessing => "Your order is being processed and packed.",
                StatusShipped => "Your order has been shipped! Use tracking number to follow your package.",
                StatusOutForDelivery => "Your order is out for delivery today! Expect it soon.",
                StatusDelivered => "Your order has been delivered. Thank you for shopping with us!",
                StatusCancelled => "This order has been cancelled. Contact support if you have questions.",
                StatusRefunded => "This order has been refunded. Funds should return within 3-5 business days.",
                StatusCompleted => "Order completed. Thank you for your business!",
                _ => "Your order is being processed."
            };
        }

        // Get progress percentage for tracking timeline
        public static int GetOrderProgressPercentage(string status)
        {
            return status switch
            {
                StatusPending => 10,
                StatusApproved => 25,
                StatusProcessing => 40,
                StatusShipped => 60,
                StatusOutForDelivery => 80,
                StatusDelivered => 100,
                StatusCancelled => 0,
                StatusRefunded => 0,
                _ => 0
            };
        }

        // Get estimated delivery days based on status
        public static int GetEstimatedDeliveryDays(string status, DateTime orderDate)
        {
            return status switch
            {
                StatusPending => 7,
                StatusApproved => 6,
                StatusProcessing => 5,
                StatusShipped => 3,
                StatusOutForDelivery => 1,
                StatusDelivered => 0,
                _ => 5
            };
        }

        // Get QR code tracking URL text
        public static string GetQrCodeTrackingText(string orderId)
        {
            return $"Scan to track order #{orderId}";
        }

        // Get status color for progress bar
        public static string GetStatusProgressBarColor(string status)
        {
            return status switch
            {
                StatusPending => "bg-warning",
                StatusApproved => "bg-primary",
                StatusProcessing => "bg-info",
                StatusShipped => "bg-primary",
                StatusOutForDelivery => "bg-info",
                StatusDelivered => "bg-success",
                StatusCancelled => "bg-danger",
                StatusRefunded => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        // Get status icon background class
        public static string GetStatusIconBackground(string status)
        {
            return status switch
            {
                StatusPending => "bg-warning bg-opacity-25",
                StatusApproved => "bg-success bg-opacity-25",
                StatusProcessing => "bg-info bg-opacity-25",
                StatusShipped => "bg-primary bg-opacity-25",
                StatusOutForDelivery => "bg-info bg-opacity-25",
                StatusDelivered => "bg-success bg-opacity-25",
                StatusCancelled => "bg-danger bg-opacity-25",
                StatusRefunded => "bg-secondary bg-opacity-25",
                _ => "bg-secondary bg-opacity-25"
            };
        }

        // ======================
        // EXISTING HELPER METHODS
        // ======================
        public static string GetOrderStatusBadgeClass(string status)
        {
            return status switch
            {
                StatusPending => "bg-warning text-dark",
                StatusApproved => "bg-success",
                StatusProcessing => "bg-info",
                StatusShipped => "bg-primary",
                StatusOutForDelivery => "bg-info text-white",
                StatusDelivered => "bg-success",
                StatusCancelled => "bg-danger",
                StatusRefunded => "bg-secondary",
                StatusCompleted => "bg-success",
                _ => "bg-secondary"
            };
        }

        public static string GetOrderStatusIcon(string status)
        {
            return status switch
            {
                StatusPending => "bi-hourglass",
                StatusApproved => "bi-check-circle",
                StatusProcessing => "bi-gear",
                StatusShipped => "bi-box-seam",
                StatusOutForDelivery => "bi-truck",
                StatusDelivered => "bi-check-circle-fill",
                StatusCancelled => "bi-x-circle",
                StatusRefunded => "bi-arrow-return-left",
                StatusCompleted => "bi-star",
                _ => "bi-question-circle"
            };
        }

        public static string GetPaymentStatusBadgeClass(string status)
        {
            return status switch
            {
                PaymentStatusPending => "bg-warning text-dark",
                PaymentStatusApproved => "bg-success",
                PaymentStatusDeferred => "bg-info",
                PaymentStatusRejected => "bg-danger",
                PaymentStatusRefunded => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        public static string GetPaymentStatusIcon(string status)
        {
            return status switch
            {
                PaymentStatusPending => "bi-clock",
                PaymentStatusApproved => "bi-check-circle",
                PaymentStatusDeferred => "bi-building",
                PaymentStatusRejected => "bi-x-circle",
                PaymentStatusRefunded => "bi-arrow-return-left",
                _ => "bi-credit-card"
            };
        }

        public static string GetSizeTypeIcon(string sizeType)
        {
            return sizeType switch
            {
                SizeTypeRegular => "bi-person",
                SizeTypeSuit => "bi-person-badge",
                SizeTypeKid => "bi-emoji-smile",
                SizeTypeShoe => "bi-box",
                _ => "bi-tag"
            };
        }

        public static string GetSizeTypeAlertClass(string sizeType)
        {
            return sizeType switch
            {
                SizeTypeRegular => "alert-info",
                SizeTypeSuit => "alert-primary",
                SizeTypeKid => "alert-success",
                SizeTypeShoe => "alert-warning",
                _ => "alert-secondary"
            };
        }

        public static string GetDeliveryEstimate(string deliveryMethod)
        {
            return deliveryMethod switch
            {
                DeliveryStandard => "3-5 business days",
                DeliveryExpress => "1-2 business days",
                DeliveryNextDay => "Next business day",
                DeliveryPickup => "Ready in 2 hours",
                _ => "3-5 business days"
            };
        }

        public static string GetTrackingUrl(string carrier, string trackingNumber)
        {
            return carrier switch
            {
                CarrierPosten => $"https://sporing.posten.no/sporing?q={trackingNumber}",
                CarrierBring => $"https://tracking.bring.com/tracking/{trackingNumber}",
                CarrierHelthjem => $"https://helthjem.no/tracking?q={trackingNumber}",
                CarrierDHL => $"https://www.dhl.com/no-en/home/tracking/tracking-parcel.html?submit=1&tracking-id={trackingNumber}",
                _ => "#"
            };
        }
    }
}