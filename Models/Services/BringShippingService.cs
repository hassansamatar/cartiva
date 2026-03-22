using Microsoft.Extensions.Configuration;
using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Services
{
    public class BringShippingService : IBringShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // ✅ Correct constructor for typed client
        public BringShippingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<BringShipmentResponse> CreateShipmentAsync(BringShipmentRequest request)
        {
            // Mock implementation (kept as placeholder)
            await Task.Delay(500);
            return new BringShipmentResponse
            {
                Success = true,
                TrackingNumber = "BRING-" + Guid.NewGuid().ToString().Substring(0, 8),
                Carrier = "Bring",
                Service = "Pakke i postkassen",
                LabelUrl = "https://bring.no/labels/mock.pdf"
            };
        }
    }
}
