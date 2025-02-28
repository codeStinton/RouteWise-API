using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Models.Amadeus;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Interfaces;
using System;
using RouteWise.Controllers.Defaults;
using System.Net;

namespace RouteWise.Services
{
    public class AmadeusService : IAmadeusService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthentication _authentication;
        private readonly IMemoryCache _cache;  // Inject IMemoryCache
        private readonly JsonSerializerOptions _jsonOptions;

        public AmadeusService(
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

        #region FlightSearch (Single-City)
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

            // Cache for, say, 10 minutes
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
        #endregion

        #region FlightSearchWithLayovers (Explore)
        public async Task<SimpleFlightOffersResponse> FlightSearchV2(
            string origin,
            string? destination,
            int? minLayoverDuration,
            int adults,
            int max,
            int? stops,
            int? maxPrice = null,
            int resultLimit = 10,
            string? userDepartureDate = null,
            string? userReturnDate = null
        )
        {
            // 1) Build a more complete cache key so changing these inputs triggers a new search.
            string baseKey = $"Explore_Base_{origin}_{adults}_{max}_{resultLimit}" +
                             $"_dep_{userDepartureDate ?? "NONE"}_ret_{userReturnDate ?? "NONE"}" +
                             $"_lay_{stops ?? -1}_minLay_{minLayoverDuration ?? -1}_maxPrice_{maxPrice ?? -1}";

            // 2) Check cache
            if (!_cache.TryGetValue(baseKey, out List<FlightOffer>? validFlightsSoFar))
            {
                var token = await _authentication.GetAccessTokenAsync();

                // 2a) Decide final departure & return dates
                string departureDateToUse = !string.IsNullOrWhiteSpace(userDepartureDate)
                    ? userDepartureDate
                    : DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");

                string returnDateToUse = !string.IsNullOrWhiteSpace(userReturnDate)
                    ? userReturnDate
                    : DateTime.UtcNow.AddDays(7 + FlightSearchDefaults.DurationDays).ToString("yyyy-MM-dd");

                // 3) Fetch a list of possible destinations (Amadeus "flight-destinations" or your own logic)
                var destinations = await GetAvailableDestinationsAsync(origin, token);
                if (destinations.Count == 0)
                {
                    throw new Exception("No valid destinations found.");
                }

                // 4) Build a final list of flights that already passed filtering
                validFlightsSoFar = new List<FlightOffer>();

                foreach (var dest in destinations)
                {
                    // If we already have enough flights, we can stop making more calls
                    if (validFlightsSoFar.Count >= resultLimit) break;

                    // Fetch up to 'max' flights for this one destination
                    var flightOptions = await FetchFlightOptionsAsync(
                        origin,
                        dest,
                        departureDateToUse,
                        returnDateToUse,
                        token,
                        adults,
                        max,
                        maxPrice
                    );

                    // Immediately filter flights for layover or stops
                    var chunkFiltered = FilterFlights(
                        new FlightSearchResponseV2 { Data = flightOptions.Data },
                        minLayoverDuration,
                        stops
                    );

                    // Add *only* as many as we need to reach 'resultLimit'
                    int needed = resultLimit - validFlightsSoFar.Count;
                    validFlightsSoFar.AddRange(chunkFiltered.Take(needed));
                }

                // 5) Store result in cache for next time
                _cache.Set(baseKey, validFlightsSoFar, TimeSpan.FromMinutes(10));
            }

            // 6) If the user specified a particular destination, we can refine here.
            //    (We've already partially filtered flights that pass layover/stops,
            //     but some flights might be for a different final destination.)
            List<FlightOffer> flightsForDestination;
            if (string.IsNullOrEmpty(destination))
            {
                flightsForDestination = validFlightsSoFar;
            }
            else
            {
                flightsForDestination = validFlightsSoFar.Where(f =>
                    f.Itineraries.Any(i => i.Segments.Last().Arrival.IataCode == destination)
                ).ToList();
            }

            // 7) Final limit in case we got more than 'resultLimit':
            var finalLimited = flightsForDestination.Take(resultLimit).ToList();

            // 8) Convert to simpler structure
            var simpleResponse = ConvertToSimpleOffersWithLayovers(
                new FlightSearchResponseV2 { Data = finalLimited }
            );

            return simpleResponse;
        }

        #endregion

        #region Days of the week search

        public async Task<SimpleFlightOffersResponse> FlightSearchByDayOfWeekForMonthOptimized(
            string origin,
            string? destination,
            int year,
            int month,
            DayOfWeek departureDayOfWeek,
            DayOfWeek returnDayOfWeek,
            int adults,
            int max, // Each call can fetch up to this many
            int? minLayoverDuration = null,
            int? layovers = null,
            int? maxPrice = null,
            int resultLimit = 10 // Final total limit
        )
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            // Example cache key
            string cacheKey = $"Optimized_{origin}_{destination}_{year}_{month}_{departureDayOfWeek}_{returnDayOfWeek}_{adults}_{max}_{minLayoverDuration}_{layovers}_{maxPrice}_{resultLimit}";
            if (_cache.TryGetValue(cacheKey, out SimpleFlightOffersResponse cached))
            {
                return cached;
            }

            var token = await _authentication.GetAccessTokenAsync();
            var validFlights = new List<FlightOffer>(); // flights that pass the filter

            // Loop each day in the month
            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
            {
                // If we already have enough valid flights, no need to fetch more
                if (validFlights.Count >= resultLimit)
                    break;

                // Skip days that don’t match departureDayOfWeek
                if (d.DayOfWeek != departureDayOfWeek)
                    continue;

                // Next day-of-week for the return flight
                var returnDate = GetNextDayOfWeek(d, returnDayOfWeek);
                if (returnDate > endDate)
                    continue;

                if (string.IsNullOrEmpty(destination))
                {
                    // If no single destination is specified, fetch for multiple destinations
                    var destinations = await GetAvailableDestinationsAsync(origin, token);
                    foreach (var dest in destinations)
                    {
                        if (validFlights.Count >= resultLimit)
                            break;

                        // Fetch up to 'max' results
                        var flightOptions = await FetchFlightOptionsAsync(
                            origin,
                            dest,
                            d.ToString("yyyy-MM-dd"),
                            returnDate.ToString("yyyy-MM-dd"),
                            token,
                            adults,
                            max,
                            maxPrice
                        );

                        // Filter them right away
                        var filteredBatch = FilterFlights(
                            new FlightSearchResponseV2 { Data = flightOptions.Data },
                            minLayoverDuration,
                            layovers
                        );

                        // Add only as many as we still need
                        int needed = resultLimit - validFlights.Count;
                        validFlights.AddRange(filteredBatch.Take(needed));
                    }
                }
                else
                {
                    // Specific destination
                    var flightOptions = await FetchFlightOptionsAsync(
                        origin,
                        destination,
                        d.ToString("yyyy-MM-dd"),
                        returnDate.ToString("yyyy-MM-dd"),
                        token,
                        adults,
                        max,
                        maxPrice
                    );

                    // Filter them right away
                    var filteredBatch = FilterFlights(
                        new FlightSearchResponseV2 { Data = flightOptions.Data },
                        minLayoverDuration,
                        layovers
                    );

                    // Add only as many as we still need
                    int needed = resultLimit - validFlights.Count;
                    validFlights.AddRange(filteredBatch.Take(needed));
                }
            }

            // At this point, 'validFlights' should have at most `resultLimit` flights
            // that already pass the layover/stops filter.
            // We can do a final clamp if desired (in case we reached exactly resultLimit in the loop):
            var limitedResults = validFlights.Take(resultLimit).ToList();

            // Convert to your final structure
            var simpleResponse = ConvertToSimpleOffersWithLayovers(
                new FlightSearchResponseV2 { Data = limitedResults }
            );

            _cache.Set(cacheKey, simpleResponse, TimeSpan.FromMinutes(10));
            return simpleResponse;
        }

        private static DateTime GetNextDayOfWeek(DateTime startDate, DayOfWeek desiredDayOfWeek)
        {
            int diff = (desiredDayOfWeek - startDate.DayOfWeek + 7) % 7;
            return startDate.AddDays(diff);
        }

        public async Task<SimpleFlightOffersResponse> FlightSearchByMonth(
            string origin,
            string? destination,
            int year,
            int month,
            int adults,
            int max,
            int? minLayoverDuration,
            int? stops,
            int? maxPrice,
            int resultLimit = 10
        )
        {
            // 1) Build a cache key that includes 'destination' (or an indication of null)
            string destKeyPart = string.IsNullOrEmpty(destination) ? "ALLDEST" : destination;
            string baseKey = $"MonthSearch_{origin}_{destKeyPart}_{year}_{month}" +
                     $"_adults_{adults}_max_{max}_res_{resultLimit}" +
                     $"_stops_{stops ?? -1}_minLay_{minLayoverDuration ?? -1}_maxPrice_{maxPrice ?? -1}";

            if (!_cache.TryGetValue(baseKey, out List<FlightOffer>? validFlightsSoFar))
            {
                validFlightsSoFar = new List<FlightOffer>();
                var token = await _authentication.GetAccessTokenAsync();

                // 2) Figure out which destinations to use
                //    - If user gave a destination, we'll just do that one.
                //    - Otherwise, call your "GetAvailableDestinationsAsync" to find all possible.
                List<string> destinationsToSearch;
                if (string.IsNullOrEmpty(destination))
                {
                    destinationsToSearch = await GetAvailableDestinationsAsync(origin, token);
                    // If no valid destinations found, you might want to return empty or throw
                        if (destinationsToSearch.Count == 0)
                    {
                        // Decide how to handle no valid destinations
                        // For example, you can return an empty SimpleFlightOffersResponse
                        return new SimpleFlightOffersResponse { Flights = new List<SimpleFlightOffer>() };
                    }
                }
                else
                {
                    destinationsToSearch = new List<string> { destination };
                }

                // 3) Loop over each day in the specified month
                var firstDay = new DateTime(year, month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1); // e.g. 2025-03-31 if (year=2025, month=3)

                // 4) For each day
                for (DateTime date = firstDay; date <= lastDay; date = date.AddDays(1))
                {
                    if (validFlightsSoFar.Count >= resultLimit) break;

                    string departureStr = date.ToString("yyyy-MM-dd");

                    // 5) For each destination in the list
                    foreach (var dest in destinationsToSearch)
                    {
                        if (validFlightsSoFar.Count >= resultLimit) break;

                        // One-way example: pass null for the return date
                        var flightOptions = await FetchFlightOptionsAsync(
                            origin,
                            dest,
                            departureStr,
                            null,
                            token,
                            adults,
                            max,
                            maxPrice
                        );

                        // Immediately filter flights
                        var chunkFiltered = FilterFlights(
                            new FlightSearchResponseV2 { Data = flightOptions.Data },
                            minLayoverDuration,
                            stops
                        );

                        // Add only as many as we need to get up to 'resultLimit'
                        int needed = resultLimit - validFlightsSoFar.Count;
                        validFlightsSoFar.AddRange(chunkFiltered.Take(needed));
                    }
                }

                // 6) Store in cache
                _cache.Set(baseKey, validFlightsSoFar, TimeSpan.FromMinutes(10));
            }

            // 7) We already only fetched flights for the user’s chosen or available destinations,
            //    so we don’t need to filter on `destination` again here.
            //    If you want to do a final filter for some reason, you can, but it's redundant.

            // 8) Limit the final result
            var finalLimited = validFlightsSoFar.Take(resultLimit).ToList();

            // 9) Convert to your simpler structure
            var simpleResponse = ConvertToSimpleOffersWithLayovers(
                new FlightSearchResponseV2 { Data = finalLimited }
            );

            return simpleResponse;
        }

        public async Task<SimpleFlightOffersResponse> FlightSearchByDuration(
             string origin,
             string? destination,
             int durationDays,
             int adults,
             int max,
             int? minLayoverDuration,
             int? stops,
             int? maxPrice,
             int resultLimit = 10
         )
        {
            // 1) Cache key that includes 'destination' or "ALLDEST"
            string destKeyPart = string.IsNullOrEmpty(destination) ? "ALLDEST" : destination;
            string baseKey = $"DurSearch_{origin}_{destKeyPart}_dur_{durationDays}" +
                     $"_adults_{adults}_max_{max}_res_{resultLimit}" +
                     $"_stops_{stops ?? -1}_minLay_{minLayoverDuration ?? -1}_maxPrice_{maxPrice ?? -1}";


            if (!_cache.TryGetValue(baseKey, out List<FlightOffer>? validFlightsSoFar))
            {
                validFlightsSoFar = new List<FlightOffer>();
                var token = await _authentication.GetAccessTokenAsync();

                // 2) Figure out which destinations to search
                List<string> destinationsToSearch;
                if (string.IsNullOrEmpty(destination))
                {
                    destinationsToSearch = await GetAvailableDestinationsAsync(origin, token);
                    if (destinationsToSearch.Count == 0)
                    {
                        // Return empty if no valid destinations
                        return new SimpleFlightOffersResponse { Flights = new List<SimpleFlightOffer>() };
                    }
                }
                else
                {
                    destinationsToSearch = new List<string> { destination };
                }

                // 3) Choose the range of departure dates: e.g., next 30 days
                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(30);

                // 4) For each possible DEPARTURE day:
                for (var departureDay = startDate; departureDay < endDate; departureDay = departureDay.AddDays(1))
                {
                    if (validFlightsSoFar.Count >= resultLimit) break;

                    string depStr = departureDay.ToString("yyyy-MM-dd");
                    string retStr = departureDay.AddDays(durationDays).ToString("yyyy-MM-dd");

                    // 5) For each possible destination
                    foreach (var dest in destinationsToSearch)
                    {
                        if (validFlightsSoFar.Count >= resultLimit) break;

                        var flightOptions = await FetchFlightOptionsAsync(
                            origin,
                            dest,
                            depStr,
                            retStr,   // round-trip for 'durationDays'
                            token,
                            adults,
                            max,
                            maxPrice
                        );

                        // Filter them
                        var chunkFiltered = FilterFlights(
                            new FlightSearchResponseV2 { Data = flightOptions.Data },
                            minLayoverDuration,
                            stops
                        );

                        int needed = resultLimit - validFlightsSoFar.Count;
                        validFlightsSoFar.AddRange(chunkFiltered.Take(needed));
                    }
                }

                _cache.Set(baseKey, validFlightsSoFar, TimeSpan.FromMinutes(10));
            }

            // 6) We already included all relevant destinations above, 
            //    so there's no final "filter by destination" needed here.

            var finalLimited = validFlightsSoFar.Take(resultLimit).ToList();
            var simpleResponse = ConvertToSimpleOffersWithLayovers(
                new FlightSearchResponseV2 { Data = finalLimited }
            );

            return simpleResponse;
        }


        #endregion

        #region Multi-City
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
        #endregion

        #region Helper Methods
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
              string origin,
              string destination,
              string departureDate,
              string? returnDate,
              string token,
              int adults,
              int max,
              int? maxPrice
          )
        {
            var url = AmadeusEndpoints.FlightOffersEndpoint
                + $"?originLocationCode={origin}"
                + $"&destinationLocationCode={destination}"
                + $"&departureDate={departureDate}"
                + $"&adults={adults}"
                + $"&max={max}";

            if (!string.IsNullOrWhiteSpace(returnDate))
            {
                url += $"&returnDate={returnDate}";
            }

            if (maxPrice.HasValue)
            {
                url += $"&maxPrice={maxPrice.Value}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            try
            {
                var response = await _httpClient.SendAsync(request);

                // If it's a non-2xx, you can either:
                // - throw, or
                // - log & return empty
                if (!response.IsSuccessStatusCode)
                {
                    return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
                }

                // If 204 No Content, just return empty
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FlightSearchResponseV2>(content, _jsonOptions);

                // If somehow null or missing Data => also return empty
                if (result == null)
                {
                    return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
                }
                if (result.Data == null)
                {
                    result.Data = new List<FlightOffer>();
                }

                return result;
            }
            catch (Exception ex)
            {
                // For any exception (network failure, JSON parse error, etc.), 
                // we log a warning and return an empty set instead of rethrowing.
                return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
            }
        }


        private static List<FlightOffer> FilterFlights(
            FlightSearchResponseV2 response,
            int? minLayoverDuration,
            int? layovers
        )
        {
            var filteredFlights = response.Data.Where(flight =>
            {
                foreach (var itinerary in flight.Itineraries)
                {
                    if (layovers.HasValue)
                    {
                        int totalLayovers = itinerary.Segments.Count - 1;
                        if (totalLayovers != layovers.Value)
                            return false;
                    }

                    if (minLayoverDuration.HasValue)
                    {
                        bool allLayoversValid = itinerary.Segments.Zip(itinerary.Segments.Skip(1), (first, second) =>
                        {
                            var layoverMinutes = (DateTime.Parse(second.Departure.At) - DateTime.Parse(first.Arrival.At)).TotalMinutes;
                            return layoverMinutes >= minLayoverDuration.Value;
                        }).All(valid => valid);

                        if (!allLayoversValid) return false;
                    }
                }
                return true;
            }).ToList();

            return filteredFlights;
        }

        private static int CalculateLayoverDuration(string arrivalTime, string nextDepartureTime)
        {
            DateTime arrival = DateTime.Parse(arrivalTime);
            DateTime nextDeparture = DateTime.Parse(nextDepartureTime);
            return (int)(nextDeparture - arrival).TotalMinutes;
        }

        private SimpleFlightOffersResponse ConvertToSimpleOffersWithLayovers(FlightSearchResponseV2 rawResponse)
        {
            var simpleResponse = new SimpleFlightOffersResponse();

            foreach (var offer in rawResponse.Data)
            {
                if (offer.Itineraries.Count == 0)
                    continue;

                var firstItinerary = offer.Itineraries.First();
                var lastItinerary = offer.Itineraries.Last();

                var outboundFirstSegment = firstItinerary.Segments.FirstOrDefault();
                if (outboundFirstSegment == null) continue;

                var outboundLastSegment = firstItinerary.Segments.LastOrDefault();
                if (outboundLastSegment == null) continue;

                var inboundLastSegment = (offer.Itineraries.Count > 1)
                    ? lastItinerary.Segments.LastOrDefault()
                    : null;

                var simpleOffer = new SimpleFlightOffer
                {
                    Origin = outboundFirstSegment.Departure.IataCode,
                    Destination = outboundLastSegment.Arrival.IataCode,
                    Price = offer.Price.GrandTotal,
                    DepartureDate = ParseDateOnly(outboundFirstSegment.Departure.At),
                    ReturnDate = (inboundLastSegment != null)
                        ? ParseDateOnly(inboundLastSegment.Arrival.At)
                        : string.Empty
                };

                // Collect layovers from all itineraries
                foreach (var itinerary in offer.Itineraries)
                {
                    for (int i = 0; i < itinerary.Segments.Count - 1; i++)
                    {
                        var current = itinerary.Segments[i];
                        var next = itinerary.Segments[i + 1];
                        int layoverMins = CalculateLayoverDuration(
                            current.Arrival.At, next.Departure.At);

                        simpleOffer.Layovers.Add(new SimpleLayover
                        {
                            Airport = current.Arrival.IataCode,
                            DurationMinutes = layoverMins,
                            ArrivalTimeOfPreviousFlight = current.Arrival.At,
                            DepartureTimeOfNextFlight = next.Departure.At
                        });
                    }
                }

                simpleResponse.Flights.Add(simpleOffer);
            }

            return simpleResponse;
        }

        private static string ParseDateOnly(string dateTime)
        {
            if (DateTime.TryParse(dateTime, out var parsed))
            {
                return parsed.ToString("yyyy-MM-dd");
            }
            return dateTime;
        }
        #endregion
    }
}
