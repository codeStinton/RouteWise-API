using RouteWise.Models.Amadeus.V2.DTOs;
using System.ComponentModel.DataAnnotations;

namespace RouteWise.DTOs.V2
{
    /// <summary>
    /// Request model for multi-city flight searches.
    /// </summary>
    public class MultiCitySearchRequestV2
    {
        /// <summary>
        /// A list of flights.
        /// </summary>
        [Required, MinLength(1)]
        public List<OriginDestinationDto> OriginDestinations { get; set; } = new();

        /// <summary>
        /// A list of travelers.
        /// </summary>
        [Required, MinLength(1)]
        public List<TravelerDto> Travelers { get; set; } = new()
        {
            new TravelerDto()
        };

        /// <summary>
        /// Flight offer sources.
        /// </summary>
        public List<string>? Sources { get; set; }

        /// <summary>
        /// Search criteria like max flight offers.
        /// </summary>
        public SearchCriteriaDto? SearchCriteria { get; set; }
    }
}