using System.Text.Json.Serialization;

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
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration until the access token expires.
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the state returned as part of the authentication response.
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
    }
}