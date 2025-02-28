using System.ComponentModel.DataAnnotations;

namespace RouteWise.Models.Amadeus.V2.DTOs
{
    public class OriginDestinationDto
    {
        /// <summary> 
        /// The airport IATA code for departure (e.g., JFK, LAX).
        /// </summary>
        [Required, MinLength(3), MaxLength(3)]
        public string OriginLocationCode { get; set; } = "JFK";

        /// <summary> 
        /// The airport IATA code for arrival (e.g., LAX, LHR).
        /// </summary>
        [Required, MinLength(3), MaxLength(3)]
        public string DestinationLocationCode { get; set; } = "LAX";

        /// <summary> 
        /// The date of departure (YYYY-MM-DD).
        /// </summary>
        [Required, DataType(DataType.Date)]
        public string DepartureDate { get; set; } = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
    }
}