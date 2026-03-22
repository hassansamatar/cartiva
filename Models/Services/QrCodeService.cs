using Microsoft.AspNetCore.Http;
using Models.Interfaces;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QrCodeService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GenerateOrderQrCode(int orderId)
        {
            var trackingUrl = GetOrderTrackingUrl(orderId);

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(trackingUrl, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception)
            {
                // Return a placeholder if QR generation fails
                return string.Empty;
            }
        }

        public byte[] GenerateOrderQrCodeBytes(int orderId)
        {
            var trackingUrl = GetOrderTrackingUrl(orderId);

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(trackingUrl, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                return qrCode.GetGraphic(20);
            }
            catch (Exception)
            {
                return Array.Empty<byte>();
            }
        }

        public string GetOrderTrackingUrl(int orderId)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return $"https://localhost:5001/Order/Track/{orderId}"; // Fallback URL
            }

            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/Order/Track/{orderId}";
        }
    }
}



