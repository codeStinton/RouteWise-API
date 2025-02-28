using RouteWise.Models.Amadeus.V2;

namespace RouteWise.Services.Interfaces
{
    public interface IFlightSearchServiceV2
    {
        Task<SimpleFlightOffersResponse> FlightSearch(
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
            int resultLimit);
    }
}
