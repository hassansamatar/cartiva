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
        public const string StatusOutForDelivery = "Out for Delivery";  // NEW
        public const string StatusDelivered = "Delivered";              // NEW
        public const string StatusCancelled = "Cancelled";
        public const string StatusRefunded = "Refunded";
        public const string StatusCompleted = "Completed";

        // ======================
        // PAYMENT STATUS CONSTANTS
        // ======================
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDeferred = "Deferred";  // For company accounts
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
        // HELPER METHODS
        // ======================
        public static string GetOrderStatusBadgeClass(string status)
        {
            return status switch
            {
                StatusPending => "bg-warning text-dark",
                StatusApproved => "bg-success",
                StatusProcessing => "bg-info",
                StatusShipped => "bg-primary",
                StatusOutForDelivery => "bg-info text-white",      // NEW
                StatusDelivered => "bg-success",                   // NEW
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
                StatusOutForDelivery => "bi-truck",                // NEW
                StatusDelivered => "bi-check-circle-fill",         // NEW
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

        // NEW: Get delivery time estimate
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

        // NEW: Get carrier tracking URL
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