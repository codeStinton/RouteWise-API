using System.ComponentModel.DataAnnotations;

namespace RouteWise.Models.Amadeus.V2.DTOs
{
    public class OriginDestinationDto
    {
        /// <summary> 
        /// The airport IATA code for departure.
        /// </summary>
        [Required, MinLength(3), MaxLength(3)]
        public required string OriginLocationCode { get; set; }

        /// <summary> 
        /// The airport IATA code for arrival.
        /// </summary>
        [Required, MinLength(3), MaxLength(3)]
        public required string DestinationLocationCode { get; set; }

        /// <summary> 
        /// The date of departure.
        /// </summary>
        [Required, DataType(DataType.Date)]
        public string DepartureDate { get; set; } = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
    }
}