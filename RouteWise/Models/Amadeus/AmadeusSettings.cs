namespace RouteWise.Models.Amadeus
{
    /// <summary>
    /// Represents the configuration settings for connecting to the Amadeus API.
    /// </summary>
    public class AmadeusSettings
    {
        /// <summary>
        /// Gets or sets the client identifier for the Amadeus API.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client secret for the Amadeus API.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration in minutes for caching API responses.
        /// </summary>
        public int CacheDurationInMinutes { get; set; } = 60;
    }

}