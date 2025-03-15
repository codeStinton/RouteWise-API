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

            // Retrieve the cached response if it exists
            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), async () =>
            {
                var token = await _authentication.GetOrRefreshAccessToken(cancellationToken);

                // Generate the flight departure & return combination
                var datePairs = BuildDatePairs(request);

                // Find flight offers based off the configuration options
                var collectedFlights = await CollectFlightOffersAsync(request.Origin, datePairs, token, request, cancellationToken);

                // Format the response
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
            // If no destionation is provided, find all possible destionations
            var destinations = string.IsNullOrWhiteSpace(request.Destination) 
                ? await BuildDestinationListAsync(origin, token, cancellationToken) 
                : [request.Destination];

            var collected = new List<FlightOffer>();

            foreach (var (Departure, Return) in datePairs)
            {
                if (collected.Count >= request.ResultLimit) break;
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var dest in destinations)
                {
                    if (collected.Count >= request.ResultLimit) break;
                    cancellationToken.ThrowIfCancellationRequested();

                    // Find the flight options for each destination
                    var flightOptions = await FetchFlightOptionsAsync(
                        origin,
                        dest,
                        Departure,
                        Return,
                        token,
                        request.Adults,
                        request.Max,
                        request.MaxPrice,
                        cancellationToken
                    );

                    if (flightOptions is null) continue;

                    List<FlightOffer> filtered;

                    // If the request includes layover configuration, apply the filtering
                    if (request.Layovers.HasValue || request.MinLayoverDuration.HasValue)
                    {
                        filtered = ApplyLayoverFilter(flightOptions, request.MinLayoverDuration, request.Layovers);
                    }
                    else
                    {
                        filtered = flightOptions.Data;
                    }
                    // Only take the request result limit
                    int needed = request.ResultLimit - collected.Count;
                    collected.AddRange(filtered.Take(needed));
                }
            }

            return collected;
        }

        private async Task<List<string>> BuildDestinationListAsync(string origin, string token, CancellationToken cancellationToken = default)
        {
            var allDestinations = await GetAvailableDestinationsAsync(origin, token, cancellationToken);
            if (allDestinations.Count == 0)
            {
                throw new FlightSearchException($"No available destinations found for origin: {origin}");
            }

            return allDestinations;
        }

        private static List<FlightOffer> ApplyLayoverFilter(
            FlightSearchResponseV2 response,
            int? minLayoverDuration,
            int? layovers
        )
        {
            return response.Data.Where(flight =>
            {
                foreach (var itinerary in flight.Itineraries)
                {
                    // Ensure the layover count matches the requested amount
                    if (layovers.HasValue)
                    {
                        int totalLayovers = itinerary.Segments.Count - 1;
                        if (totalLayovers != layovers.Value)
                            return false;
                    }

                    // Ensure the minimum layover duration matches the requested amount
                    if (minLayoverDuration.HasValue && itinerary.Segments.Count > 1)
                    {
                        bool allLayoversValid = true;
                        for (int i = 0; i < itinerary.Segments.Count - 1; i++)
                        {
                            // Find the layover arrival and departure times
                            if (DateTime.TryParse(itinerary.Segments[i].Arrival.At, out DateTime arrival) &&
                                DateTime.TryParse(itinerary.Segments[i + 1].Departure.At, out DateTime departure))
                            {
                                // Calculate the layover duration
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
            string origin,
            string destination,
            string departureDate,
            string? returnDate,
            string token,
            int adults,
            int max,
            int? maxPrice,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheExtensions.GenerateCacheKey(
                "FlightOffers", origin, destination, departureDate, returnDate, adults, max, maxPrice);

            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), async () =>
            {
                try
                {
                    var url = Helpers.UriBuilder.FlightOffers(origin, destination, departureDate, returnDate, adults, max, maxPrice);
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

            // Retrieve the cached response if it exists
            return await _cache.GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(_cacheDurationMinutes), async () =>
            {
                var url = Helpers.UriBuilder.FligthDestinations(origin);
                return await _httpClient.GetAsync<List<string>>(url, token, _jsonOptions, cancellationToken);
            });
        }
    }
}
