using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RouteWise.Caching;
using RouteWise.DTOs.V2;
using RouteWise.Models.Amadeus;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Helpers;
using RouteWise.Services.Interfaces;
using CacheExtensions = RouteWise.Caching.CacheExtensions;

namespace RouteWise.Services
{
    public class MultiCityServiceV2 : IMultiCityServiceV2
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly IAuthentication _authentication;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly int _cacheDurationMinutes;

        public MultiCityServiceV2(IMemoryCache cache, HttpClient httpClient, IAuthentication authentication, IOptions<JsonSerializerOptions> jsonOptions, IOptions<AmadeusSettings> options)
        {
            _cache = cache;
            _httpClient = httpClient;
            _authentication = authentication;
            _jsonOptions = jsonOptions.Value;
            _cacheDurationMinutes = options.Value.CacheDurationInMinutes;
        }

        /// <inheritdoc/>
        public async Task<FlightSearchResponseV2> MultiCityFlightSearch(MultiCitySearchRequestV2 request, CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheExtensions.GenerateCacheKey("MultiCityFlightSearch", request);

            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), async () =>
            {
                var token = await _authentication.GetOrRefreshAccessToken(cancellationToken);
                var bodyString = BuildMultiCityRequestBody(request);

                return await _httpClient.PostAsync<FlightSearchResponseV2>(AmadeusEndpoints.FlightOffersEndpoint, token, bodyString, _jsonOptions, cancellationToken);
            });
        }

        private static string BuildMultiCityRequestBody(MultiCitySearchRequestV2 request)
        {
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

            return JsonSerializer.Serialize(requestBody);
        }
    }
}
