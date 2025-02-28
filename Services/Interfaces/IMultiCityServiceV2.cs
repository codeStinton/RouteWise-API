using RouteWise.Models.Amadeus.V2;

namespace RouteWise.Services.Interfaces
{
    public interface IMultiCityServiceV2
    {
        Task<FlightSearchResponseV2> MultiCityFlightSearch(MultiCitySearchRequest request);
    }
}
