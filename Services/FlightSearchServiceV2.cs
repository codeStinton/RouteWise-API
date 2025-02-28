using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Interfaces;
using RouteWise.Controllers.Defaults;
using System.Net;

namespace RouteWise.Services
{
    public class FlightSearchServiceV2 : IFlightSearchServiceV2
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthentication _authentication;
        private readonly IMemoryCache _cache;  // Inject IMemoryCache
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightSearchServiceV2(HttpClient httpClient, IAuthentication authentication, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _authentication = authentication;
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<SimpleFlightOffersResponse> FlightSearch(
            string origin,
            string? destination,
            int? year,
            int? month,
            DayOfWeek? departureDayOfWeek,
            DayOfWeek? returnDayOfWeek,
            int? durationDays,
            string? explicitDepartureDate,
            string? explicitReturnDate,
            int? minLayoverDuration,
            int? layovers,
            int? maxPrice,
            int adults,
            int max,
            int resultLimit)
        {
            string baseKey = $"Unif_{origin}_{destination}_{year}_{month}_{departureDayOfWeek}_{returnDayOfWeek}"
                           + $"_{durationDays}_{explicitDepartureDate}_{explicitReturnDate}"
                           + $"_{minLayoverDuration}_{layovers}_{maxPrice}_{adults}_{max}_{resultLimit}";

            if (_cache.TryGetValue(baseKey, out SimpleFlightOffersResponse? cachedResponse))
            {
                return cachedResponse;
            }

            var token = await _authentication.GetAccessTokenAsync();

            var datePairs = BuildDatePairs(
                year, month,
                departureDayOfWeek, returnDayOfWeek,
                durationDays,
                explicitDepartureDate, explicitReturnDate);

            var destinations = await BuildDestinationListAsync(origin, destination, token);

            var collectedFlights = await CollectFlightOffersAsync(
                origin,
                destinations,
                datePairs,
                token,
                adults,
                max,
                maxPrice,
                minLayoverDuration,
                layovers,
                resultLimit);

            var finalResponse = ConvertToSimpleOffersWithLayovers(
                new FlightSearchResponseV2 { Data = collectedFlights }
            );

            _cache.Set(baseKey, finalResponse, TimeSpan.FromMinutes(10));

            return finalResponse;
        }

        private List<(string Departure, string? Return)> BuildDatePairs(
            int? year,
            int? month,
            DayOfWeek? departureDayOfWeek,
            DayOfWeek? returnDayOfWeek,
            int? durationDays,
            string? explicitDeparture,
            string? explicitReturn)
        {
            var datePairs = new List<(string, string?)>();

            if (year.HasValue && month.HasValue
                && departureDayOfWeek.HasValue
                && returnDayOfWeek.HasValue)
            {
                DateTime startDate = new(year.Value, month.Value, 1);
                DateTime endDate = new(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value));

                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != departureDayOfWeek.Value) continue;
                    DateTime returnCandidate = GetNextDayOfWeek(d, returnDayOfWeek.Value);
                    if (returnCandidate <= endDate)
                    {
                        datePairs.Add((d.ToString("yyyy-MM-dd"), returnCandidate.ToString("yyyy-MM-dd")));
                    }
                }
                return datePairs;
            }

            if (year.HasValue && month.HasValue)
            {
                DateTime firstDay = new(year.Value, month.Value, 1);
                DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

                for (DateTime d = firstDay; d <= lastDay; d = d.AddDays(1))
                {
                    datePairs.Add((d.ToString("yyyy-MM-dd"), null));
                }
                return datePairs;
            }

            if (durationDays.HasValue)
            {
                var now = DateTime.UtcNow.Date;
                var end = now.AddDays(30);

                for (var d = now; d < end; d = d.AddDays(1))
                {
                    datePairs.Add(
                        (d.ToString("yyyy-MM-dd"),
                         d.AddDays(durationDays.Value).ToString("yyyy-MM-dd"))
                    );
                }
                return datePairs;
            }

            if (!string.IsNullOrWhiteSpace(explicitDeparture) && !string.IsNullOrWhiteSpace(explicitReturn))
            {
                datePairs.Add((explicitDeparture!, explicitReturn!));
                return datePairs;
            }

            var defaultDep = DateTime.UtcNow.AddDays(7);
            var defaultRet = defaultDep.AddDays(FlightSearchDefaults.DurationDays);
            datePairs.Add((defaultDep.ToString("yyyy-MM-dd"), defaultRet.ToString("yyyy-MM-dd")));

            return datePairs;
        }

        private async Task<List<string>> BuildDestinationListAsync(
            string origin,
            string? destination,
            string token)
        {
            if (!string.IsNullOrEmpty(destination))
            {
                return new List<string> { destination };
            }

            var dests = await GetAvailableDestinationsAsync(origin, token);
            if (dests.Count == 0)
            {
                return new List<string>();
            }

            return dests;
        }

        private async Task<List<FlightOffer>> CollectFlightOffersAsync(
            string origin,
            List<string> destinations,
            List<(string Departure, string? Return)> datePairs,
            string token,
            int adults,
            int max,
            int? maxPrice,
            int? minLayoverDuration,
            int? layovers,
            int resultLimit)
        {
            var collected = new List<FlightOffer>();

            foreach (var pair in datePairs)
            {
                if (collected.Count >= resultLimit) break;

                foreach (var dest in destinations)
                {
                    if (collected.Count >= resultLimit) break;

                    var flightOptions = await FetchFlightOptionsAsync(
                        origin,
                        dest,
                        pair.Departure,
                        pair.Return,
                        token,
                        adults,
                        max,
                        maxPrice
                    );

                    var filtered = FilterFlights(flightOptions, minLayoverDuration, layovers);

                    int needed = resultLimit - collected.Count;
                    collected.AddRange(filtered.Take(needed));
                }
            }

            return collected;
        }

        private static DateTime GetNextDayOfWeek(DateTime startDate, DayOfWeek desiredDayOfWeek)
        {
            int diff = (desiredDayOfWeek - startDate.DayOfWeek + 7) % 7;
            return startDate.AddDays(diff);
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
                        bool allLayoversValid = itinerary.Segments
                            .Zip(itinerary.Segments.Skip(1), (first, second) =>
                            {
                                var layoverMinutes = (DateTime.Parse(second.Departure.At)
                                                      - DateTime.Parse(first.Arrival.At)).TotalMinutes;
                                return layoverMinutes >= minLayoverDuration.Value;
                            })
                            .All(valid => valid);

                        if (!allLayoversValid) return false;
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
                url += $"&returnDate={returnDate}";

            if (maxPrice.HasValue)
                url += $"&maxPrice={maxPrice.Value}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
                }

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FlightSearchResponseV2>(content, _jsonOptions)
                             ?? new FlightSearchResponseV2 { Data = new List<FlightOffer>() };

                if (result.Data == null)
                {
                    result.Data = new List<FlightOffer>();
                }
                return result;
            }
            catch
            {
                return new FlightSearchResponseV2 { Data = new List<FlightOffer>() };
            }
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

            if (destinations?.Data == null) return new List<string>();
            return destinations.Data.Select(d => d.Destination).ToList();
        }

        private SimpleFlightOffersResponse ConvertToSimpleOffersWithLayovers(FlightSearchResponseV2 rawResponse)
        {
            var simpleResponse = new SimpleFlightOffersResponse();

            foreach (var offer in rawResponse.Data)
            {
                if (offer.Itineraries.Count == 0) continue;

                var firstItinerary = offer.Itineraries.First();
                var outboundFirstSegment = firstItinerary.Segments.FirstOrDefault();
                var outboundLastSegment = firstItinerary.Segments.LastOrDefault();
                if (outboundFirstSegment == null || outboundLastSegment == null) continue;

                var lastItinerary = offer.Itineraries.Last();
                var inboundLastSegment = (offer.Itineraries.Count > 1)
                    ? lastItinerary.Segments.LastOrDefault()
                    : null;

                var simpleOffer = new SimpleFlightOffer
                {
                    Origin = outboundFirstSegment.Departure.IataCode,
                    Destination = outboundLastSegment.Arrival.IataCode,
                    Price = offer.Price.GrandTotal,
                    DepartureDate = ParseDateOnly(outboundFirstSegment.Departure.At),
                    ReturnDate = inboundLastSegment != null
                        ? ParseDateOnly(inboundLastSegment.Arrival.At)
                        : string.Empty
                };

                foreach (var itinerary in offer.Itineraries)
                {
                    for (int i = 0; i < itinerary.Segments.Count - 1; i++)
                    {
                        var current = itinerary.Segments[i];
                        var next = itinerary.Segments[i + 1];

                        int layoverMins = CalculateLayoverDuration(current.Arrival.At, next.Departure.At);

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

        private static int CalculateLayoverDuration(string arrivalTime, string nextDepartureTime)
        {
            DateTime arrival = DateTime.Parse(arrivalTime);
            DateTime nextDeparture = DateTime.Parse(nextDepartureTime);
            return (int)(nextDeparture - arrival).TotalMinutes;
        }

        private static string ParseDateOnly(string dateTime)
        {
            if (DateTime.TryParse(dateTime, out var parsed))
            {
                return parsed.ToString("yyyy-MM-dd");
            }
            return dateTime;
        }
    }
}
