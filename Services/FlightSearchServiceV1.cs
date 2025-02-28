using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Interfaces;

namespace RouteWise.Services
{
    public class FlightSearchServiceV1 : IV1FlightSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthentication _authentication;
        private readonly IMemoryCache _cache;  // Inject IMemoryCache
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightSearchServiceV1(
            HttpClient httpClient,
            IAuthentication authentication,
            IMemoryCache cache // We'll inject memory cache here
        )
        {
            _httpClient = httpClient;
            _authentication = authentication;
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<FlightSearchResponse> FlightSearch(
             string origin,
             int? maxPrice = null,
             bool? oneWay = null,
             string? departureDate = null,
             int? duration = null,
             bool? nonStop = null)
        {
            // Build a cache key that includes all these parameters
            string cacheKey = $"FlightSearch_{origin}_{maxPrice}_{oneWay}_{departureDate}_{duration}_{nonStop}";

            if (_cache.TryGetValue(cacheKey, out FlightSearchResponse cachedResponse))
            {
                return cachedResponse;
            }

            var token = await _authentication.GetAccessTokenAsync();
            var url = BuildFlightDestinationsUrl(origin, maxPrice, oneWay, departureDate, duration, nonStop);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FlightSearchResponse>(content, _jsonOptions);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            return result!;
        }

        private static string BuildFlightDestinationsUrl(
            string origin, int? maxPrice, bool? oneWay, string? departureDate, int? duration, bool? nonStop)
        {
            var queryParams = new List<string> { $"origin={origin}" };
            if (maxPrice.HasValue) queryParams.Add($"maxPrice={maxPrice.Value}");
            if (oneWay.HasValue) queryParams.Add($"oneWay={oneWay.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(departureDate)) queryParams.Add($"departureDate={departureDate}");
            if (oneWay == true && duration.HasValue) queryParams.Add($"duration={duration.Value}");
            if (nonStop.HasValue) queryParams.Add($"nonStop={nonStop.Value.ToString().ToLower()}");

            var queryString = string.Join("&", queryParams);
            return $"{AmadeusEndpoints.FlightDestinationsEndpoint}?{queryString}";
        }
    }
}
