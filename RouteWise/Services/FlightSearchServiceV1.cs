using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Services.Interfaces;
using RouteWise.Models.Amadeus;
using Microsoft.Extensions.Options;
using RouteWise.Caching;
using CacheExtensions = RouteWise.Caching.CacheExtensions;
using RouteWise.DTOs.V1;
using RouteWise.Services.Helpers;

namespace RouteWise.Services
{
    public class FlightSearchServiceV1 : IFlightSearchServiceV1
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly int _cacheDurationMinutes;
        private readonly IAuthentication _authentication;
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightSearchServiceV1(IMemoryCache cache, HttpClient httpClient, IAuthentication authentication, IOptions<JsonSerializerOptions> jsonOptions, IOptions<AmadeusSettings> options)
        {
            _cache = cache;
            _httpClient = httpClient;
            _authentication = authentication;
            _jsonOptions = jsonOptions.Value;
            _cacheDurationMinutes = options.Value.CacheDurationInMinutes;
        }

        /// <inheritdoc/>
        public async Task<FlightSearchResponseV1> FlightSearch(string origin, FlightSearchRequestV1 request, CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheExtensions.GenerateCacheKey("FlightSearchV1", origin, request);

            // Retrieve the cached response if it exists
            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), 
                async () =>
                {
                    var token = await _authentication.GetOrRefreshAccessToken(cancellationToken);
                    var url = Helpers.UriBuilder.FligthDestinations(origin, request.MaxPrice, request.OneWay, request.DepartureDate, request.Duration, request.NonStop);

                    return await _httpClient.GetAsync<FlightSearchResponseV1>(url, token, _jsonOptions, cancellationToken);
                });
        }
    }
}