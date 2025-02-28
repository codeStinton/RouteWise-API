using System.Text;
using System.Text.Json;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Interfaces;

namespace RouteWise.Services
{
    public class MultiCityServiceV2 : IMultiCityServiceV2
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthentication _authentication;
        private readonly JsonSerializerOptions _jsonOptions;

        public MultiCityServiceV2(HttpClient httpClient, IAuthentication authentication)
        {
            _httpClient = httpClient;
            _authentication = authentication;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<FlightSearchResponseV2> MultiCityFlightSearch(MultiCitySearchRequest request)
        {
            var token = await _authentication.GetAccessTokenAsync();

            var requestBody = new
            {
                originDestinations = request.OriginDestinations.Select((segment, index) => new
                {
                    id = (index + 1).ToString(),
                    originLocationCode = segment.OriginLocationCode,
                    destinationLocationCode = segment.DestinationLocationCode,
                    departureDateTimeRange = new { date = segment.DepartureDate }
                }).ToList(),

                travelers = request.Travelers.Select((traveler, index) => new
                {
                    id = (index + 1).ToString(),
                    travelerType = traveler.TravelerType.ToString(),
                    fareOptions = traveler.FareOptions
                }).ToList(),

                sources = request.Sources,

                searchCriteria = new
                {
                    maxFlightOffers = request.SearchCriteria.MaxFlightOffers
                }
            };

            var bodyString = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var url = "https://test.api.amadeus.com/v2/shopping/flight-offers";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("Authorization", $"Bearer {token}");
            httpRequest.Content = new StringContent(bodyString, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Amadeus API error: {errorResponse}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FlightSearchResponseV2>(responseContent, _jsonOptions)!;
        }
    }
}
