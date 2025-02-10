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

        Task<FlightSearchResponseV2> FlightSearchWithLayovers(
            string origin, int duration, int minLayoverDuration);
    }
}
