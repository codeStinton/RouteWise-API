namespace RouteWise.Models.Amadeus
{
    /// <summary>
    /// Represents an error response containing details about an error.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the HTTP status code associated with the error.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a message that describes the error.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the request identifier, used for tracking and troubleshooting.
        /// </summary>
        public string RequestId { get; set; } = string.Empty;
    }
}