using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Shared
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
        public const string StatusAwaitingShipmentApproval = "Awaiting Shipment Approval";   // NEW
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
        public const string PaymentStatusPaid = "Paid";

        // ======================
        // INVOICE CONSTANTS
        // ======================
        public const int DeferredPaymentDays = 30;
        public const int InvoiceReminderDaysBeforeDue = 7;
        public const int InvoiceOverdueGraceDays = 3;

        // Norwegian VAT rates (MVA)
        public const decimal VatRateStandard = 25.00m;    // Standard rate
        public const decimal VatRateReduced = 15.00m;     // Food, transport
        public const decimal VatRateLow = 12.00m;         // Cinema, sports events
        public const decimal VatRateZero = 0.00m;         // Exempt

        // Default currency
        public const string DefaultCurrency = "NOK";

        // Invoice number prefix
        public const string InvoiceNumberPrefix = "INV";
        public const string CreditNoteNumberPrefix = "CN";

        // ======================
        // VAT CALCULATION HELPERS
        // ======================

        /// <summary>
        /// Calculates price excluding VAT from a price including VAT
        /// </summary>
        public static decimal CalculatePriceExVat(decimal priceIncVat, decimal vatRate = VatRateStandard)
        {
            return priceIncVat / (1 + vatRate / 100m);
        }

        /// <summary>
        /// Calculates price including VAT from a price excluding VAT
        /// </summary>
        public static decimal CalculatePriceIncVat(decimal priceExVat, decimal vatRate = VatRateStandard)
        {
            return priceExVat * (1 + vatRate / 100m);
        }

        /// <summary>
        /// Calculates VAT amount from a price excluding VAT
        /// </summary>
        public static decimal CalculateVatAmount(decimal priceExVat, decimal vatRate = VatRateStandard)
        {
            return priceExVat * (vatRate / 100m);
        }

        /// <summary>
        /// Calculates VAT amount from a price including VAT
        /// </summary>
        public static decimal CalculateVatFromInclusivePrice(decimal priceIncVat, decimal vatRate = VatRateStandard)
        {
            return priceIncVat - CalculatePriceExVat(priceIncVat, vatRate);
        }

        /// <summary>
        /// Calculates discount amount from a percentage
        /// </summary>
        public static decimal CalculateDiscountAmount(decimal originalPrice, decimal discountPercent)
        {
            return originalPrice * (discountPercent / 100m);
        }

        /// <summary>
        /// Applies discount and returns the discounted price
        /// </summary>
        public static decimal ApplyDiscount(decimal originalPrice, decimal discountPercent)
        {
            return originalPrice - CalculateDiscountAmount(originalPrice, discountPercent);
        }

        /// <summary>
        /// Gets VAT breakdown for a given price (returns tuple: exVat, vatAmount, incVat)
        /// </summary>
        public static (decimal ExVat, decimal VatAmount, decimal IncVat) GetVatBreakdown(
            decimal priceExVat, decimal vatRate = VatRateStandard)
        {
            var vatAmount = CalculateVatAmount(priceExVat, vatRate);
            return (priceExVat, vatAmount, priceExVat + vatAmount);
        }

        /// <summary>
        /// Gets VAT breakdown from an inclusive price (returns tuple: exVat, vatAmount, incVat)
        /// </summary>
        public static (decimal ExVat, decimal VatAmount, decimal IncVat) GetVatBreakdownFromInclusivePrice(
            decimal priceIncVat, decimal vatRate = VatRateStandard)
        {
            var exVat = CalculatePriceExVat(priceIncVat, vatRate);
            var vatAmount = priceIncVat - exVat;
            return (exVat, vatAmount, priceIncVat);
        }

        /// <summary>
        /// Formats a price with currency for display (Norwegian format)
        /// </summary>
        public static string FormatPrice(decimal amount, string currency = DefaultCurrency)
        {
            return $"{amount:N2} {currency}";
        }

        /// <summary>
        /// Formats a price with VAT info for display
        /// </summary>
        public static string FormatPriceWithVat(decimal priceIncVat, decimal vatRate = VatRateStandard, string currency = DefaultCurrency)
        {
            var exVat = CalculatePriceExVat(priceIncVat, vatRate);
            return $"{priceIncVat:N2} {currency} (inkl. {vatRate:N0}% MVA)";
        }

        // ======================
        // INVOICE STATUS CONSTANTS (string versions)
        // ======================
        public const string InvoiceStatusDraft = "Draft";
        public const string InvoiceStatusIssued = "Issued";
        public const string InvoiceStatusSent = "Sent";
        public const string InvoiceStatusPaid = "Paid";
        public const string InvoiceStatusPartiallyPaid = "PartiallyPaid";
        public const string InvoiceStatusOverdue = "Overdue";
        public const string InvoiceStatusCancelled = "Cancelled";

        // ======================
        // CREDIT NOTE STATUS CONSTANTS (string versions)
        // ======================
        public const string CreditNoteStatusDraft = "Draft";
        public const string CreditNoteStatusIssued = "Issued";
        public const string CreditNoteStatusBooked = "Booked";
        public const string CreditNoteStatusCancelled = "Cancelled";

        // ======================
        // SHIPMENT STATUS CONSTANTS (NEW)
        // ======================
        public const string ShipmentStatusPendingApproval = "Pending Approval";
        public const string ShipmentStatusApproved = "Approved";
        public const string ShipmentStatusShipped = "Shipped";
        public const string ShipmentStatusDelivered = "Delivered";
        public const string ShipmentStatusCancelled = "Cancelled";

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
        // RETURN CONSTANTS
        // ======================
        public const int ReturnWindowDays = 30;

        public const string ReturnStatusPending = "Pending";
        public const string ReturnStatusApproved = "Approved";
        public const string ReturnStatusRejected = "Rejected";
        public const string ReturnStatusRefunded = "Refunded";

        public const string ReturnReasonDefective = "Defective or damaged";
        public const string ReturnReasonWrongItem = "Wrong item received";
        public const string ReturnReasonDoesNotFit = "Does not fit";
        public const string ReturnReasonNotAsDescribed = "Not as described";
        public const string ReturnReasonChangedMind = "Changed my mind";
        public const string ReturnReasonOther = "Other";

        public static string GetReturnStatusBadgeClass(string status)
        {
            return status switch
            {
                ReturnStatusPending => "bg-warning text-dark",
                ReturnStatusApproved => "bg-info",
                ReturnStatusRejected => "bg-danger",
                ReturnStatusRefunded => "bg-success",
                _ => "bg-secondary"
            };
        }

        public static string[] GetReturnReasons()
        {
            return new[]
            {
                ReturnReasonDefective,
                ReturnReasonWrongItem,
                ReturnReasonDoesNotFit,
                ReturnReasonNotAsDescribed,
                ReturnReasonChangedMind,
                ReturnReasonOther
            };
        }

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
                StatusAwaitingShipmentApproval => "Your order is waiting for shipment approval. We'll notify you soon.",
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
                StatusAwaitingShipmentApproval => 45,
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
                StatusAwaitingShipmentApproval => 5,
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
                StatusAwaitingShipmentApproval => "bg-info",
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
                StatusAwaitingShipmentApproval => "bg-info bg-opacity-25",
                StatusShipped => "bg-primary bg-opacity-25",
                StatusOutForDelivery => "bg-info bg-opacity-25",
                StatusDelivered => "bg-success bg-opacity-25",
                StatusCancelled => "bg-danger bg-opacity-25",
                StatusRefunded => "bg-secondary bg-opacity-25",
                _ => "bg-secondary bg-opacity-25"
            };
        }

        // ======================
        // SHIPMENT STATUS HELPERS (NEW)
        // ======================
        public static string GetShipmentStatusBadgeClass(string status)
        {
            return status switch
            {
                ShipmentStatusPendingApproval => "bg-warning text-dark",
                ShipmentStatusApproved => "bg-success",
                ShipmentStatusShipped => "bg-primary",
                ShipmentStatusDelivered => "bg-success",
                ShipmentStatusCancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public static string GetShipmentStatusIcon(string status)
        {
            return status switch
            {
                ShipmentStatusPendingApproval => "bi-hourglass",
                ShipmentStatusApproved => "bi-check-circle",
                ShipmentStatusShipped => "bi-box-seam",
                ShipmentStatusDelivered => "bi-check-circle-fill",
                ShipmentStatusCancelled => "bi-x-circle",
                _ => "bi-question-circle"
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
                StatusAwaitingShipmentApproval => "bg-info text-white",
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
                StatusAwaitingShipmentApproval => "bi-clock-history",
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

        // ======================
        // INVOICE HELPERS
        // ======================
        public static string GetInvoiceStatusBadgeClass(string status)
        {
            return status switch
            {
                InvoiceStatusDraft => "bg-secondary",
                InvoiceStatusIssued => "bg-info",
                InvoiceStatusSent => "bg-primary",
                InvoiceStatusPaid => "bg-success",
                InvoiceStatusPartiallyPaid => "bg-warning text-dark",
                InvoiceStatusOverdue => "bg-danger",
                InvoiceStatusCancelled => "bg-dark",
                _ => "bg-secondary"
            };
        }

        public static string GetInvoiceStatusIcon(string status)
        {
            return status switch
            {
                InvoiceStatusDraft => "bi-file-earmark",
                InvoiceStatusIssued => "bi-file-earmark-check",
                InvoiceStatusSent => "bi-send",
                InvoiceStatusPaid => "bi-check-circle-fill",
                InvoiceStatusPartiallyPaid => "bi-pie-chart",
                InvoiceStatusOverdue => "bi-exclamation-triangle",
                InvoiceStatusCancelled => "bi-x-circle",
                _ => "bi-file-earmark"
            };
        }

        public static string GetCreditNoteStatusBadgeClass(string status)
        {
            return status switch
            {
                CreditNoteStatusDraft => "bg-secondary",
                CreditNoteStatusIssued => "bg-info",
                CreditNoteStatusBooked => "bg-success",
                CreditNoteStatusCancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public static string GetCreditNoteStatusIcon(string status)
        {
            return status switch
            {
                CreditNoteStatusDraft => "bi-file-earmark",
                CreditNoteStatusIssued => "bi-file-earmark-minus",
                CreditNoteStatusBooked => "bi-journal-check",
                CreditNoteStatusCancelled => "bi-x-circle",
                _ => "bi-file-earmark"
            };
        }

        /// <summary>
        /// Checks if a user is eligible for deferred payment (active company user)
        /// </summary>
        public static bool IsEligibleForDeferredPayment(string? companyId, bool? isCompanyActive)
        {
            return !string.IsNullOrEmpty(companyId) && isCompanyActive == true;
        }

        /// <summary>
        /// Generates an invoice number with prefix and date
        /// Format: INV-2025-00001
        /// </summary>
        public static string GenerateInvoiceNumber(int sequence)
        {
            return $"{InvoiceNumberPrefix}-{DateTime.UtcNow.Year}-{sequence:D5}";
        }

        /// <summary>
        /// Generates a credit note number with prefix and date
        /// Format: CN-2025-00001
        /// </summary>
        public static string GenerateCreditNoteNumber(int sequence)
        {
            return $"{CreditNoteNumberPrefix}-{DateTime.UtcNow.Year}-{sequence:D5}";
        }

        /// <summary>
        /// Generates a KID number for Norwegian bank payments (Mod10 checksum)
        /// </summary>
        public static string GenerateKIDNumber(int invoiceId)
        {
            string baseNumber = invoiceId.ToString().PadLeft(15, '0');
            int sum = 0;
            bool alternate = true;
            for (int i = baseNumber.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(baseNumber[i].ToString());
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                alternate = !alternate;
            }
            int checksum = (sum * 9) % 10;
            return baseNumber + checksum.ToString();
        }

        /// <summary>
        /// Returns the due date for a deferred payment invoice
        /// </summary>
        public static DateOnly GetDeferredPaymentDueDate(DateTime orderDate)
        {
            return DateOnly.FromDateTime(orderDate.AddDays(DeferredPaymentDays));
        }
    }
}