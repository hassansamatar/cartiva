using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cartiva.Infrastructure.AddressService
{
    public class AddressLookupService
    {
        private readonly HttpClient _httpClient;

        public AddressLookupService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Searches Norwegian addresses via Kartverket API.
        /// Returns JSON string. Never returns null.
        /// </summary>
        public async Task<string> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "{}"; // Return empty JSON if query is empty

            var url = $"https://ws.geonorge.no/adresser/v1/sok?sok={query}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // API failed, return empty JSON object
                    return "{}";
                }

                var content = await response.Content.ReadAsStringAsync();

                // Ensure content is not null
                return string.IsNullOrWhiteSpace(content) ? "{}" : content;
            }
            catch
            {
                // Network or other error, return empty JSON
                return "{}";
            }
        }
    }
}
