using RouteWise.DTOs.V2;
using RouteWise.Models.Amadeus.V2;

namespace RouteWise.Services.Interfaces
{
    public interface IMultiCityServiceV2
    {
        /// <summary>
        /// Performs a multi-city flight search based on the given request parameters.
        /// </summary>
        /// <param name="request">The multi-city search request containing search parameters for multiple destinations.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the multi-city flight search response.
        /// </returns>
        Task<FlightSearchResponseV2> MultiCityFlightSearch(MultiCitySearchRequestV2 request, CancellationToken cancellationToken = default);
    }
}
