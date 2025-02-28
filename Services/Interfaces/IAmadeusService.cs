using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;

namespace RouteWise.Services.Interfaces
{
    public interface IAmadeusService
    {
        Task<FlightSearchResponse> FlightSearch(
            string origin,
            int? maxPrice = null,
            bool? oneWay = null,
            string? departureDate = null,
            int? duration = null,
            bool? nonStop = null);

        Task<SimpleFlightOffersResponse> FlightSearchV2(
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
        );

        Task<SimpleFlightOffersResponse> FlightSearchByDayOfWeekForMonthOptimized(
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
        );

        Task<SimpleFlightOffersResponse> FlightSearchByMonth(
            string origin,
            string? destination,
            int year,
            int month,
            int adults,
            int max,
            int? minLayoverDuration,
            int? stops,
            int? maxPrice = null,
            int resultLimit = 10
        );

        Task<SimpleFlightOffersResponse> FlightSearchByDuration(
            string origin,
            string? destination,
            int durationDays,
            int adults,
            int max,
            int? minLayoverDuration,
            int? stops,
            int? maxPrice = null,
            int resultLimit = 10
        );

        Task<FlightSearchResponseV2> MultiCityFlightSearch(MultiCitySearchRequest request);
    }
}
