using RouteWise.Controllers.Defaults;
using System.ComponentModel.DataAnnotations;

namespace RouteWise.DTOs.V2
{
    public class SearchCriteriaDto
    {
        /// <summary>
        /// Maximum number of flight offers to return.
        /// </summary>
        [Range(1, 50)]
        public int MaxFlightOffers { get; set; } = FlightSearchDefaults.TotalResultLimit;
    }
}