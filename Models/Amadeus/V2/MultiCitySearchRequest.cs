using RouteWise.Models.Amadeus.V2.DTOs;
using System.ComponentModel.DataAnnotations;

namespace RouteWise.Models.Amadeus.V2
{
    public class MultiCitySearchRequest
    {
        /// <summary>
        /// A list of flights (e.g., New York → London → Paris → Rome).
        /// </summary>
        [Required, MinLength(1)]
        public List<OriginDestinationDto> OriginDestinations { get; set; } = new()
        {
            new OriginDestinationDto() // Pre-fill example
        };

        /// <summary>
        /// A list of travelers.
        /// </summary>
        [Required, MinLength(1)]
        public List<TravelerDto> Travelers { get; set; } = new()
        {
            new TravelerDto() // Pre-fill example
        };

        /// <summary>
        /// Flight offer sources (e.g., "GDS").
        /// </summary>
        public List<string> Sources { get; set; } = new() { "GDS" };

        /// <summary>
        /// Search criteria like max flight offers.
        /// </summary>
        public SearchCriteriaDto SearchCriteria { get; set; } = new();
    }
}