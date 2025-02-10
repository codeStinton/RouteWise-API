using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Models.Amadeus;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Interfaces;

namespace RouteWise.Services
{
    public class AmadeusService : IAmadeusService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly IAuthentication _authentication;

        public AmadeusService(HttpClient httpClient, IAuthentication authentication)
        {
            _httpClient = httpClient;
            _authentication = authentication;
        }

        public async Task<FlightSearchResponse> FlightSearch(
             string origin,
             int? maxPrice = null,
             bool? oneWay = null,
             string? departureDate = null,
             int? duration = null,
             bool? nonStop = null)
        {

            var token = await _authentication.GetAccessTokenAsync();

            var queryParams = new List<string>
            {
                $"origin={origin}"
            };

            if (maxPrice.HasValue)
                queryParams.Add($"maxPrice={maxPrice.Value}");

            if (oneWay.HasValue)
                queryParams.Add($"oneWay={oneWay.Value.ToString().ToLower()}");

            if (!string.IsNullOrEmpty(departureDate))
                queryParams.Add($"departureDate={departureDate}");

            if (oneWay == true && duration.HasValue)
                queryParams.Add($"duration={duration.Value}");

            if (nonStop.HasValue)
                queryParams.Add($"nonStop={nonStop.Value.ToString().ToLower()}");

            var queryString = string.Join("&", queryParams);
            var url = $"{AmadeusEndpoints.FlightDestinationsEndpoint}?{queryString}";

            var request = new HttpRequestMessage(HttpMethod.Get, url); // todo: send request method
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FlightSearchResponse>(content)!;
        }

        public async Task<FlightSearchResponseV2> FlightSearchWithLayovers(
            string origin, int duration, int minLayoverDuration)
        {
            var token = await _authentication.GetAccessTokenAsync();

            var departureDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
            var returnDate = DateTime.UtcNow.AddDays(7 + duration).ToString("yyyy-MM-dd");

            var destinations = await GetAvailableDestinationsAsync(origin, token);
            if (destinations == null || destinations.Count == 0)
            {
                throw new Exception("No valid destinations found.");
            }

            var flightResults = new List<FlightOffer>();

            foreach (var destination in destinations)
            {
                var flightOptions = await FetchFlightOptionsAsync(origin, destination, departureDate, returnDate, token);

                var validFlights = FilterFlights(flightOptions, minLayoverDuration);
                flightResults.AddRange(validFlights);
            }

            return new FlightSearchResponseV2 { Data = flightResults };
        }

        private async Task<List<string>> GetAvailableDestinationsAsync(string origin, string token)
        {
            var url = $"{AmadeusEndpoints.FlightDestinationsEndpoint}?origin={origin}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var destinations = JsonSerializer.Deserialize<FlightDestinationResponse>(responseContent, _jsonOptions);

            return destinations?.Data?.Select(d => d.Destination).ToList() ?? new List<string>();
        }

        private async Task<FlightSearchResponseV2> FetchFlightOptionsAsync(
            string origin, string destination, string departureDate, string returnDate, string token)
        {
            var url = AmadeusEndpoints.FlightOffersEndpoint
                    + $"?originLocationCode={origin}"
                    + $"&destinationLocationCode={destination}"
                    + $"&departureDate={departureDate}"
                    + $"&returnDate={returnDate}"
                    + $"&adults=1"
                    + $"&max=20";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FlightSearchResponseV2>(responseContent, _jsonOptions)!;
        }

        private List<FlightOffer> FilterFlights(FlightSearchResponseV2 response, int minLayoverDuration)
        {
            return response.Data.Where(flight =>
            {
                foreach (var itinerary in flight.Itineraries)
                {
                    for (int i = 0; i < itinerary.Segments.Count - 1; i++)
                    {
                        var currentSegment = itinerary.Segments[i];
                        var nextSegment = itinerary.Segments[i + 1];

                        int layoverMinutes = CalculateLayoverDuration(currentSegment.Arrival.At, nextSegment.Departure.At);

                        if (layoverMinutes >= minLayoverDuration)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }).ToList();
        }

        private int CalculateLayoverDuration(string arrivalTime, string nextDepartureTime)
        {
            DateTime arrival = DateTime.Parse(arrivalTime);
            DateTime nextDeparture = DateTime.Parse(nextDepartureTime);
            return (int)(nextDeparture - arrival).TotalMinutes;
        }
    }
}
