using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Interfaces;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Models.Services
{
    public class BringShippingService : IBringShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BringShippingService> _logger;

        public BringShippingService(HttpClient httpClient, IConfiguration configuration, ILogger<BringShippingService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<BringShipmentResponse> CreateShipmentAsync(BringShipmentRequest request)
        {
            var apiUid = _configuration["Bring:ApiUid"];
            var apiKey = _configuration["Bring:ApiKey"];
            var clientUrl = _configuration["Bring:ClientUrl"];
            var customerNumber = _configuration["Bring:CustomerNumber"]; // "5" for test
            var senderName = _configuration["Bring:SenderName"];
            var senderAddress = _configuration["Bring:SenderAddress"];
            var senderPostalCode = _configuration["Bring:SenderPostalCode"];
            var senderCity = _configuration["Bring:SenderCity"];
            var senderCountry = _configuration["Bring:SenderCountry"];

            // Log configuration values
            _logger.LogInformation($"ApiUid: {apiUid}");
            _logger.LogInformation($"ApiKey: {apiKey}");
            _logger.LogInformation($"ClientUrl: {clientUrl}");
            _logger.LogInformation($"CustomerNumber: {customerNumber}");

            // Fallback to mock if credentials missing
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUid))
            {
                _logger.LogWarning("Bring API credentials missing. Using mock response.");
                return MockResponse(request);
            }

            // Build XML payload – use correct product ID for domestic parcel
            var xml = BuildShipmentXml(request, customerNumber, senderName, senderAddress,
                                       senderPostalCode, senderCity, senderCountry);

            _logger.LogInformation("Sending XML to Bring: {Xml}", xml);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "shipments")
            {
                Headers = {
                    { "X-Mybring-API-Uid", apiUid },
                    { "X-Mybring-API-Key", apiKey },
                    { "X-Bring-Client-URL", clientUrl ?? "http://localhost:7212" }
                },
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            };

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Bring response status: {StatusCode}, body: {Response}",
                                         response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    return new BringShipmentResponse
                    {
                        Success = false,
                        ErrorMessage = $"Bring API error: {response.StatusCode} - {responseContent}"
                    };
                }

                // Parse XML response
                var doc = XDocument.Parse(responseContent);
                var ns = XNamespace.Get("http://schema.bring.com/shipping/shipmentResponse");

                var consignmentNumber = doc.Descendants(ns + "consignmentNumber")?.FirstOrDefault()?.Value;
                var labelUrl = doc.Descendants(ns + "labelUrl")?.FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(consignmentNumber))
                {
                    return new BringShipmentResponse
                    {
                        Success = false,
                        ErrorMessage = "No consignment number in Bring response"
                    };
                }

                return new BringShipmentResponse
                {
                    Success = true,
                    TrackingNumber = consignmentNumber,
                    Carrier = "Bring",
                    Service = "Pakke i postkassen", // adjust as needed
                    LabelUrl = labelUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Bring API");
                return new BringShipmentResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string BuildShipmentXml(BringShipmentRequest request, string customerNumber,
                                        string senderName, string senderAddress,
                                        string senderPostalCode, string senderCity, string senderCountry)
        {
            var weightGrams = (int)(request.Weight * 1000);

            // Use product ID 5800 for domestic parcel (Pickup Point)
            // For other services, adjust as needed (5600 for home delivery, etc.)
            var productId = "5800";

            var doc = new XDocument(
                new XElement("shipmentRequest",
                    new XAttribute(XNamespace.Xmlns + "shipmentRequest", "http://schema.bring.com/shipping/shipmentRequest"),
                    new XElement("consignments",
                        new XElement("consignment",
                            new XElement("shippingDateTime", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")),
                            new XElement("products",
                                new XElement("product",
                                    new XElement("id", productId),
                                    new XElement("customerNumber", customerNumber),
                                    new XElement("weight", weightGrams),
                                    new XElement("packageType", request.PackageType ?? "BOX")
                                )
                            ),
                            new XElement("parties",
                                new XElement("sender",
                                    new XElement("name", senderName),
                                    new XElement("addressLine1", senderAddress),
                                    new XElement("postalCode", senderPostalCode),
                                    new XElement("city", senderCity),
                                    new XElement("countryCode", senderCountry)
                                ),
                                new XElement("recipient",
                                    new XElement("name", request.CustomerName),
                                    new XElement("addressLine1", request.CustomerAddress),
                                    new XElement("postalCode", request.CustomerPostalCode),
                                    new XElement("city", request.CustomerCity),
                                    new XElement("countryCode", request.CustomerCountry),
                                    new XElement("reference", $"Order #{request.OrderNumber}"),
                                    new XElement("mobile", request.CustomerPhone)
                                )
                            )
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private BringShipmentResponse MockResponse(BringShipmentRequest request)
        {
            Task.Delay(500).Wait();
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