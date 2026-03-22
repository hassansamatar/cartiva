using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Interfaces
{
    public interface IQrCodeService
    {
        /// <summary>
        /// Generates QR code as Base64 string for embedding in HTML
        /// </summary>
        string GenerateOrderQrCode(int orderId);

        /// <summary>
        /// Generates QR code as byte array for PDF generation
        /// </summary>
        byte[] GenerateOrderQrCodeBytes(int orderId);

        /// <summary>
        /// Gets the tracking URL for an order
        /// </summary>
        string GetOrderTrackingUrl(int orderId);
    }
}
