using System.ComponentModel.DataAnnotations;

namespace RouteWise.Models.Amadeus.V2.DTOs
{
    public class TravelerDto
    {
        /// <summary> 
        /// Type of traveler
        /// </summary>
        [Required]
        public TravelerType TravelerType { get; set; } = TravelerType.ADULT;

        /// <summary> 
        /// Fare options.
        /// </summary>
        public List<string> FareOptions { get; set; } = new() { "STANDARD" };
    }

    /// <summary>
    /// Enum for traveler type
    /// </summary>
    public enum TravelerType
    {
        ADULT,
        CHILD
    }
}