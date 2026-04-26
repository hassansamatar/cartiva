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
        public const int ReturnWindowDays = 14;

        public const string ReturnReasonDefective = "Defective or damaged";
        public const string ReturnReasonWrongItem = "Wrong item received";
        public const string ReturnReasonDoesNotFit = "Does not fit";
        public const string ReturnReasonNotAsDescribed = "Not as described";
        public const string ReturnReasonChangedMind = "Changed my mind";
        public const string ReturnReasonOther = "Other";

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

        // Get QR code tracking URL text
        public static string GetQrCodeTrackingText(string orderId)
        {
            return $"Scan to track order #{orderId}";
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