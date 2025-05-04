using RouteWise.DTOs.V2;
using RouteWise.Models.Amadeus.V2;

namespace RouteWise.Services.Interfaces
{
    public interface IFlightSearchServiceV2
    {
        /// <summary>
        /// Performs a flight search based on the given request parameters and returns a list of formatted flight offers.
        /// </summary>
        /// <param name="request">The flight search request containing search parameters.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of formatted flight offers.
        /// </returns>
        Task<List<FormattedFlightOffer>> FlightSearch(FlightSearchRequestV2 request, CancellationToken cancellationToken = default);
    }
}
