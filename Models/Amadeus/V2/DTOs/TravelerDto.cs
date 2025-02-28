using System.ComponentModel.DataAnnotations;

namespace RouteWise.Models.Amadeus.V2.DTOs
{
    public class TravelerDto
    {
        /// <summary> 
        /// Type of traveler (ADULT, CHILD).
        /// </summary>
        [Required]
        public TravelerType TravelerType { get; set; } = TravelerType.ADULT;

        /// <summary> 
        /// Fare options (STANDARD, FLEXIBLE).
        /// </summary>
        public List<string> FareOptions { get; set; } = new() { "STANDARD" };
    }

    /// <summary>
    /// Enum for traveler type (Creates a dropdown in Swagger UI).
    /// </summary>
    public enum TravelerType
    {
        ADULT,
        CHILD
    }
}