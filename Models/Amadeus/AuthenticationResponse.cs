namespace RouteWise.Models.Amadeus
{
    /// <summary>
    /// Represents the response returned after authenticating with the Amadeus API.
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>
        /// Gets or sets the access token used to authenticate API requests.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration until the access token expires.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the state returned as part of the authentication response.
        /// </summary>
        public string State { get; set; } = string.Empty;
    }
}
