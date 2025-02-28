using RouteWise.Models.Amadeus.V1;

namespace RouteWise.Services.Interfaces
{
    public interface IV1FlightSearchService
    {
        Task<FlightSearchResponse> FlightSearch(
            string origin,
            int? maxPrice = null,
            bool? oneWay = null,
            string? departureDate = null,
            int? duration = null,
            bool? nonStop = null);
    }
}
