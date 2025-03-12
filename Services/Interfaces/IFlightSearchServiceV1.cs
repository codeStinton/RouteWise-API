using RouteWise.DTOs.V1;
using RouteWise.Models.Amadeus.V1;

namespace RouteWise.Services.Interfaces
{
    public interface IFlightSearchServiceV1
    {
        /// <summary>
        /// Performs a flight search based on the given origin and search request parameters.
        /// </summary>
        /// <param name="origin">The origin airport code.</param>
        /// <param name="request">The flight search request containing search parameters.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the flight search response.
        /// </returns>
        Task<FlightSearchResponseV1> FlightSearch(string origin, FlightSearchRequestV1 request, CancellationToken cancellationToken = default);
    }
}
