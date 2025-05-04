using System.Text.Json;
using Microsoft.Extensions.Options;
using RouteWise.Models.Amadeus;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Interfaces;
using RouteWise.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using RouteWise.Caching;

namespace RouteWise.Services
{
    public class Authentication : IAuthentication
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly AmadeusSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public Authentication(IMemoryCache cache, HttpClient httpClient, IOptions<JsonSerializerOptions> jsonOptions, IOptions<AmadeusSettings> settings)
        {
            _cache = cache;
            _httpClient = httpClient;
            _settings = settings.Value;
            _jsonOptions = jsonOptions.Value;
        }

        /// <inheritdoc/>
        public async Task<string> GetOrRefreshAccessToken(CancellationToken cancellationToken = default)
        {
            // Retrieve the cached access token if it exists, else get a new one
            return await _cache.GetOrCreateWithEntryAsync("AccessToken", async entry =>
            {
                // Build up request params including the credentials
                var parameters = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _settings.ClientId },
                    { "client_secret", _settings.ClientSecret }
                };

                using var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(AmadeusEndpoints.AuthenticationEndpoint, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new AuthenticationException($"Authentication failed with status code {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent, _jsonOptions)
                                   ?? throw new AuthenticationException("Failed to deserialize authentication response.");

                // Verify that the token has been approved.
                if (!string.Equals(authResponse.State, "approved", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AuthenticationException($"Authentication not approved: {authResponse.State}");
                }

                // 30 second buffer on the expiration of the token
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(authResponse.ExpiresIn - 30);

                return authResponse.AccessToken;
            });
        }
    }
}