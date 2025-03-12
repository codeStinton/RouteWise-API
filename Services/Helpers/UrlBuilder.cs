using Microsoft.AspNetCore.WebUtilities;
using RouteWise.Services.Endpoints;

namespace RouteWise.Services.Helpers
{
    /// <summary>
    /// Provides methods for building Amedeus API URLs
    /// </summary>
    public static class UrlBuilder
    {
        /// <summary>
        /// Builds a URL to fetch flight destinations.
        /// </summary>
        /// <returns>
        /// A URL string with the appropriate query parameters for fetching flight destinations.
        /// </returns>
        public static string FligthDestinations(
            string origin,
            int? maxPrice = null,
            bool? oneWay = null,
            string? departureDate = null,
            int? duration = null,
            bool? nonStop = null)
        {
            // Create a dictionary for query parameters
            var queryParams = new Dictionary<string, string>
            {
                ["origin"] = origin
            };

            if (maxPrice.HasValue)
                queryParams["maxPrice"] = maxPrice.Value.ToString();
            if (oneWay.HasValue)
                queryParams["oneWay"] = oneWay.Value.ToString().ToLower();
            if (!string.IsNullOrEmpty(departureDate))
                queryParams["departureDate"] = departureDate;
            if (oneWay == true && duration.HasValue)
                queryParams["duration"] = duration.Value.ToString();
            if (nonStop.HasValue)
                queryParams["nonStop"] = nonStop.Value.ToString().ToLower();

            return QueryHelpers.AddQueryString(AmadeusEndpoints.FlightDestinationsEndpoint, queryParams);
        }

        /// <summary>
        /// Builds a URL to fetch flight offers.
        /// </summary>
        /// <returns>
        /// A URL string with the appropriate query parameters for fetching flight offers.
        /// </returns>
        public static string FlightOffers(
            string origin,
            string destination,
            string departureDate,
            string? returnDate,
            int adults,
            int max,
            int? maxPrice)
        {
            // Create a dictionary for query parameters
            var queryParams = new Dictionary<string, string>
            {
                ["originLocationCode"] = origin,
                ["destinationLocationCode"] = destination,
                ["departureDate"] = departureDate,
                ["adults"] = adults.ToString(),
                ["max"] = max.ToString()
            };

            // Conditionally add returnDate if provided
            if (!string.IsNullOrWhiteSpace(returnDate))
            {
                queryParams["returnDate"] = returnDate;
            }

            // Conditionally add maxPrice if provided
            if (maxPrice.HasValue)
            {
                queryParams["maxPrice"] = maxPrice.Value.ToString();
            }

            // Build and return the full URL for Flight Offers Endpoint
            return QueryHelpers.AddQueryString(AmadeusEndpoints.FlightOffersEndpoint, queryParams);
        }
    }
}
