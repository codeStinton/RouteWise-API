using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Interfaces;
using RouteWise.DTOs.V2;
using Microsoft.Extensions.Options;
using RouteWise.Models.Amadeus;
using RouteWise.Caching;
using CacheExtensions = RouteWise.Caching.CacheExtensions;
using RouteWise.Services.Helpers;
using RouteWise.Exceptions;

namespace RouteWise.Services
{
    public class FlightSearchServiceV2 : IFlightSearchServiceV2
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly int _cacheDurationMinutes;
        private readonly IAuthentication _authentication;
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightSearchServiceV2(IMemoryCache cache, HttpClient httpClient, IAuthentication authentication, IOptions<JsonSerializerOptions> jsonOptions, IOptions<AmadeusSettings> options)
        {
            _cache = cache;
            _httpClient = httpClient;
            _authentication = authentication;
            _jsonOptions = jsonOptions.Value;
            _cacheDurationMinutes = options.Value.CacheDurationInMinutes;
        }

        /// <inheritdoc/>
        public async Task<List<FormattedFlightOffer>> FlightSearch(FlightSearchRequestV2 request, CancellationToken cancellationToken = default)
        {
            var cacheKey = CacheExtensions.GenerateCacheKey("FlightSearchV2", request);

            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), async () =>
            {
                var token = await _authentication.GetOrRefreshAccessToken(cancellationToken);

                var datePairs = BuildDatePairs(request);
                var collectedFlights = await CollectFlightOffersAsync(request.Origin, datePairs, token, request, cancellationToken);
                var finalResponse = FormattedFlightOffersResponse.ConvertToSimpleOffersWithLayovers(new FlightSearchResponseV2 { Data = collectedFlights });

                if (finalResponse.Flights.Count == 0)
                {
                    throw new FlightSearchException("No flight offers found for the given search criteria.");
                }

                return finalResponse.Flights;
            });
        }

        private static List<(string Departure, string? Return)> BuildDatePairs(FlightSearchRequestV2 request)
        {
            if (DatePairBuilder.HasYearMonthAndDays(request))
                return DatePairBuilder.BuildDatePairsForYearMonthAndDays(request);

            if (DatePairBuilder.HasYearAndMonth(request))
                return DatePairBuilder.BuildDatePairsForYearAndMonth(request);

            if (DatePairBuilder.HasOnlyDurationDays(request))
                return DatePairBuilder.BuildDatePairsForDuration(request.DurationDays.Value);

            if (DatePairBuilder.HasDateValues(request))
                return [(request.DepartureDate!, request.ReturnDate!)];

            return DatePairBuilder.BuildDefaultDatePairs();
        }

        private async Task<List<FlightOffer>> CollectFlightOffersAsync(string origin, List<(string Departure, string? Return)> datePairs, string token, FlightSearchRequestV2 request, CancellationToken cancellationToken = default)
        {
            var destinations = await BuildDestinationListAsync(origin, request.Destination, token, cancellationToken);

            var collected = new List<FlightOffer>();

            foreach (var dates in datePairs)
            {
                if (collected.Count >= request.ResultLimit) break;
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var dest in destinations)
                {
                    if (collected.Count >= request.ResultLimit) break;
                    cancellationToken.ThrowIfCancellationRequested();

                    var flightOptions = await FetchFlightOptionsAsync(
                        origin,
                        token,
                        request,
                        cancellationToken
                    );

                    if (flightOptions is null) continue;

                    List<FlightOffer> filtered;
                    if (request.Layovers.HasValue || request.MinLayoverDuration.HasValue)
                    {
                        filtered = FilterFlights(flightOptions, request.MinLayoverDuration, request.Layovers);
                    }
                    else
                    {
                        filtered = flightOptions.Data;
                    }
                    int needed = request.ResultLimit - collected.Count;
                    collected.AddRange(filtered.Take(needed));
                }
            }

            return collected;
        }

        private async Task<List<string>> BuildDestinationListAsync(string origin, string? destination, string token, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(destination))
            {
                return [destination];
            }

            var allDestinations = await GetAvailableDestinationsAsync(origin, token, cancellationToken);
            if (allDestinations.Count == 0)
            {
                throw new FlightSearchException($"No available destinations found for origin: {origin}");
            }

            return allDestinations;
        }

        private static List<FlightOffer> FilterFlights(
            FlightSearchResponseV2 response,
            int? minLayoverDuration,
            int? layovers
        )
        {
            return response.Data.Where(flight =>
            {
                foreach (var itinerary in flight.Itineraries)
                {
                    if (layovers.HasValue)
                    {
                        int totalLayovers = itinerary.Segments.Count - 1;
                        if (totalLayovers != layovers.Value)
                            return false;
                    }

                    if (minLayoverDuration.HasValue && itinerary.Segments.Count > 1)
                    {
                        bool allLayoversValid = true;
                        for (int i = 0; i < itinerary.Segments.Count - 1; i++)
                        {
                            if (DateTime.TryParse(itinerary.Segments[i].Arrival.At, out DateTime arrival) &&
                                DateTime.TryParse(itinerary.Segments[i + 1].Departure.At, out DateTime departure))
                            {
                                double layoverMinutes = (departure - arrival).TotalMinutes;
                                if (layoverMinutes < minLayoverDuration.Value)
                                {
                                    allLayoversValid = false;
                                    break;
                                }
                            }
                            else
                            {
                                allLayoversValid = false;
                                break;
                            }
                        }
                        if (!allLayoversValid)
                            return false;
                    }
                }
                return true;
            }).ToList();
        }

        private async Task<FlightSearchResponseV2> FetchFlightOptionsAsync(
            string origin, string token, FlightSearchRequestV2 request, CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheExtensions.GenerateCacheKey(
                "FlightOffers", origin, request.Destination, request.DepartureDate, request.ReturnDate, request.Adults, request.Max, request.MaxPrice);

            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), async () =>
            {
                try
                {
                    var url = UrlBuilder.FlightOffers(origin, request.Destination, request.DepartureDate, request.ReturnDate, request.Adults, request.Max, request.MaxPrice);
                    return await _httpClient.GetAsync<FlightSearchResponseV2>(url, token, _jsonOptions, cancellationToken);

                }
                catch
                {
                    return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
                }
            });
        }

        private async Task<List<string>> GetAvailableDestinationsAsync(string origin, string token, CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheExtensions.GenerateCacheKey("AvailableDestinations", origin);

            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromHours(_cacheDurationMinutes), async () =>
            {
                var url = UrlBuilder.FligthDestinations(origin);
                return await _httpClient.GetAsync<List<string>>(url, token, _jsonOptions, cancellationToken);
            });
        }
    }
}
